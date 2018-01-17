using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NXOpen;
using NXOpen.Annotations;
using NXOpen.CAM;
using NXOpen.Drawings;
using NXOpen.Layer;
using NXOpen.UF;
using TechDocNS.Model;

namespace TechDocNS.Services
{
    public class NxDrawingCreator
    {
        static ListingWindow lw;
        private UFSession ufs;
        private int _operationNumber;
        private int _currentSheetNumber;
        private DrawingSheet _firstSheet;
        private DrawingSheet _currentSheet;
        private readonly Part _part;
        int row = 0;
        int rowNums = 0;
        Tag tabnote = Tag.Null;
        Tag firstTable = Tag.Null;
        private string _additionalToolName;
        private string _additionalToolGroupName;
        private List<string> _sparkToolGroupNotes;

        public NxDrawingCreator(NxSession nxSession)
        {
            _part = NxSession.Part;
            ufs = NxSession.Ufs;
        }

        public void CreateDrawnings(IEnumerable<DrawingsSetup> ds)
        {
            try
            {
                var sheetNums = 0;
                foreach (var drawingsSetup in ds)
                    switch (drawingsSetup.DrawingsType)
                    {
                        case 0:
                            _additionalToolName = drawingsSetup.AdditionalToolName;
                            _additionalToolGroupName = drawingsSetup.AdditionalToolGroupName;
                            _sparkToolGroupNotes = drawingsSetup.SparkToolGroupNotes;
                            break;
                        case 1:
                            CreateToolDrawings(drawingsSetup);
                            break;
                        case 2:
                            CreateSetupDrawnings(drawingsSetup);
                            break;
                        case 3:
                            // Карты эскизов будут считаться отдельно
                            sheetNums = _currentSheetNumber;
                            _currentSheetNumber = 0;
                            CreateSketchDrawnings(drawingsSetup);
                            break;
                    }

                // Выводим кол-во листов, без учета карт эскизов
                var firstSheetNoteValue = sheetNums > 0 ? sheetNums : _currentSheetNumber;
                if (_firstSheet != null)
                    SetFirstSheetNotes(_firstSheet, firstSheetNoteValue);

                var viewCollection = _part.Views;
                if (viewCollection != null)
                    viewCollection.Regenerate();
            }
            catch (Exception e)
            {
                throw;
            }
        }


        /// <summary>
        ///  Создает листы с картами инструмента
        /// </summary>
        /// <param name="ds"></param>
        private void CreateToolDrawings(DrawingsSetup ds)
        {
            _operationNumber = ds.OperationNumber;

            bool answer;
            ufs.Cam.IsSessionInitialized(out answer);
            if (!answer) ufs.Cam.InitSession();

            if (ds.DrawingsType != 1)
                throw new Exception("Ошибка вызова метода создания листа для типа " + ds.DrawingsType);


            if (!ds.NxOperationGroups.Any())
                throw new Exception("Не удалось получить выделенные объекты из навигатора операций!");

//            var groups = ds.AdditionalTools.OfType<NCGroup>().ToList();
//            var operations = ds.AdditionalTools.OfType<Operation>().ToList();
//
//            if (!groups.Any() && !operations.Any())
//                throw new Exception("Ошибка вызова метода создания листа. Выбраный объект не является операцией или группой");
//
//            // сначала получим все родительские выделенные группы операций
//            var ncGroups = groups
//                .Where(gr =>
//                    !groups.Any(g =>
//                        g.GetMembers().Contains(gr)));
//
//            // если ни одна из групп операций не содержит выделенную операцию, добавим ее к группам
//            var taggedObjects = ncGroups
//                .Concat(operations
//                .Where(op =>
//                    !groups.Any(gr => gr.GetMembers().Contains(op)))
//                .Cast<TaggedObject>());
//
//            var operationGroups = taggedObjects
//                .Select(o => new NxOperationGroup(o, _additionalToolName));

            var descriptionsService = new NxOperationDescriptionsService();
            var descriptions = descriptionsService.GetDescriptions(ds.NxOperationGroups);

            AddDescriptionsToSheets(descriptions.ToList(), ds.DrawingsFormats);   //ВНИМАНИЕ!!! Создаём КИ!
        }


        /// <summary>
        /// Создает листы с картами наладки
        /// </summary>
        /// <param name="ds"></param>
        public void CreateSetupDrawnings(DrawingsSetup ds)
        {
            _operationNumber = ds.OperationNumber;

            for (var i = 0; i < ds.SheetNums; i++)
            {
                var sheet = _currentSheetNumber == 0
                    ? CreateSheet(_part, ds.DrawingsFormats.FirstOrDefault(df => df.DrawingType == 2 && df.IsFirstSheet))
                    : CreateSheet(_part, ds.DrawingsFormats.FirstOrDefault(df => df.DrawingType == 2 && !df.IsFirstSheet));

                if (!string.IsNullOrEmpty(ds.AdditionalFile))
                    AddTechNotes(ds.AdditionalFile, ds.DrawingsFormatName);

                if (_sparkToolGroupNotes != null)
                    AddSparkToolGroupNotes(_sparkToolGroupNotes, ds.DrawingsFormatName);

                if (sheet != null)
                    SetSheetNotes(sheet);
            }
        }


        /// <summary>
        /// Создает листы с картами эскизов
        /// </summary>
        /// <param name="ds"></param>
        public void CreateSketchDrawnings(DrawingsSetup ds)
        {
            _operationNumber = ds.OperationNumber;

            for (var i = 0; i < ds.SheetNums; i++)
            {
                var sheet = i == 0
                    ? CreateSheet(_part, ds.DrawingsFormats.FirstOrDefault(df => df.DrawingType == 3 && df.IsFirstSheet))
                    : CreateSheet(_part, ds.DrawingsFormats.FirstOrDefault(df => df.DrawingType == 3 && !df.IsFirstSheet));

                if (!string.IsNullOrEmpty(ds.AdditionalFile))
                    AddTechNotes(ds.AdditionalFile, ds.DrawingsFormatName);

                if (sheet != null)
                    SetSheetNotes(sheet);

                // Сразу обозначим кол-во листов эскизов
                SetFirstSheetNotes(sheet, ds.SheetNums);
            }
        }

        /// <summary>
        /// Добавляет тех.требования из файла 
        /// с заданными настройками в определенные координаты
        /// </summary>
        /// <param name="textFile"></param>
        /// <param name="formatName"></param>
        private void AddTechNotes(string textFile, string formatName)
        {
            if (string.IsNullOrEmpty(textFile) || !File.Exists(textFile)) return;
            var strings = File.ReadAllLines(textFile, Encoding.Default);

            var letteringPreferences = _part.Annotations.Preferences.GetLetteringPreferences();
            var lettering = letteringPreferences.GetGeneralText();
            lettering.Size = 3.5;
            lettering.Cfw.Width = LineWidth.Thin;
            letteringPreferences.SetGeneralText(lettering);
            letteringPreferences.AlignmentPosition = AlignmentPosition.BottomRight;

            switch (formatName)
            {
                case "A3":
                    _part.Annotations.CreateNote(strings, new Point3d(410.0, 12.0, 0.0), AxisOrientation.Horizontal, letteringPreferences, null);
                    break;
                default:
                    _part.Annotations.CreateNote(strings, new Point3d(290.0, 12.0, 0.0), AxisOrientation.Horizontal, letteringPreferences, null);
                    break;
            }
        }

        /// <summary>
        /// Только для электроискровых станков.
        /// Добавляет надпись в верхний левый угол.
        /// Похож на метод добавления тех.требований
        /// </summary>
        /// <param name="list"></param>
        /// <param name="formatName"></param>
        private void AddSparkToolGroupNotes(IEnumerable<string> list, string formatName)
        {
            var strings = new List<string> { "Станок " + _additionalToolName };

            if (list != null) strings.AddRange(list);

            var letteringPreferences = _part.Annotations.Preferences.GetLetteringPreferences();
            var lettering = letteringPreferences.GetGeneralText();
            lettering.Size = 3.5;
            lettering.Cfw.Width = LineWidth.Thin;
            letteringPreferences.SetGeneralText(lettering);
            letteringPreferences.AlignmentPosition = AlignmentPosition.BottomRight;

            switch (formatName)
            {
                case "A3":
                    _part.Annotations.CreateNote(strings.ToArray(), new Point3d(410.0, 245, 0.0), AxisOrientation.Horizontal, letteringPreferences, null);
                    break;
                default:
                    _part.Annotations.CreateNote(strings.ToArray(), new Point3d(290.0, 150, 0.0), AxisOrientation.Horizontal, letteringPreferences, null);
                    break;
            }
        }

        /// <summary>
        /// Создает листы с картами инструментов
        /// Заполняет таблицу описанием операций
        /// </summary>
        /// <param name="operationDescriptions"></param>
        /// <param name="drawingsFormats"></param>
        private void AddDescriptionsToSheets(List<string[]> operationDescriptions, NxDrawingsFromat[] drawingsFormats)
        {
            var layerManager = _part.Layers;
            if (layerManager.WorkLayer != 256) layerManager.SetState(256, State.WorkLayer);

            foreach (var descr in operationDescriptions)
            {
                if (tabnote == Tag.Null)
                {
                    _currentSheet = CreateSheet(_part, drawingsFormats.FirstOrDefault(df => df.DrawingType == 1 && df.IsFirstSheet));
                    if (_currentSheet != null) SetSheetNotes(_currentSheet);
                    if (_firstSheet == null) _firstSheet = _currentSheet;
                    tabnote = GetTableSectionTag("КАРТА_ИНСТРУМЕНТОВ", _currentSheet);
                    ufs.Tabnot.AskNmRows(tabnote, out rowNums);
                }

                if (row >= rowNums)
                {
                    _currentSheet = CreateSheet(_part, drawingsFormats.FirstOrDefault(df => df.DrawingType == 1 && !df.IsFirstSheet));
                    if (_currentSheet != null) SetSheetNotes(_currentSheet);
                    tabnote = GetTableSectionTag("КАРТА_ИНСТРУМЕНТОВ", _currentSheet);
                    ufs.Tabnot.AskNmRows(tabnote, out rowNums);
                    row = 0;
                }

                int columnNums;
                ufs.Tabnot.AskNmColumns(tabnote, out columnNums);
                Tag rowTag;
                ufs.Tabnot.AskNthRow(tabnote, row++, out rowTag);
                var col = 0;
                foreach (var s in descr)
                {
                    Tag colTag, cell;
                    ufs.Tabnot.AskNthColumn(tabnote, col++, out colTag);
                    ufs.Tabnot.AskCellAtRowCol(rowTag, colTag, out cell);
                    ufs.Tabnot.SetCellText(cell, s);
                    if (col > columnNums) break;
                }

            }

            layerManager.SetState(1, State.WorkLayer);
            layerManager.SetState(256, State.Visible);
        }

        /// <summary>
        /// Находит таг таблицы на листе
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="drawingSheet"></param>
        /// <returns></returns>
        private Tag GetTableSectionTag(string tableName, DrawingSheet drawingSheet)
        {
            // Находим таблицу на листе
            var table = drawingSheet.View.AskVisibleObjects().OfType<TableSection>().FirstOrDefault(t => t.Name.Contains(tableName));
            if (table == null)
                throw new Exception("Не удалось найти на листе таблицу с именем " + tableName);

            // Возвращаем таг таблицы
            Tag tableNote;
            ufs.Tabnot.AskTabularNoteOfSection(table.Tag, out tableNote);
            if (tableNote == Tag.Null) throw new Exception("Ошибка при создании таблицы на странице!");
            return tableNote;
        }

        /// <summary>
        /// Создает новый лист. Параметр указывает, является ли лист первым (для разных форматов)
        /// </summary>
        /// <param name="part"></param>
        /// <param name="drawingsFromat"></param>
        /// <returns></returns>
        private DrawingSheet CreateSheet(Part part, NxDrawingsFromat drawingsFromat)
        {
            _currentSheetNumber += 1;

            if (drawingsFromat == null)
                throw new Exception("Не найдены форматки для создания листа!");

            //            var strings = Directory.GetFiles(NxSession.UGII_TEMPLATE_DIR, drawingsFromat.Template, SearchOption.AllDirectories);
            var template = System.IO.Path.Combine(NxSession.UGII_TEMPLATE_DIR, drawingsFromat.Template);
            if (!File.Exists(template))
                throw new Exception(string.Format("Не существует файл шаблона форматки {0} в директории {1}", drawingsFromat.Template, NxSession.UGII_TEMPLATE_DIR));

            DrawingSheet drawingSheet = null;

            var builder = part.DrawingSheets.DrawingSheetBuilder(drawingSheet);
            builder.Option = DrawingSheetBuilder.SheetOption.UseTemplate;
            builder.AutoStartViewCreation = false;
            builder.Units = DrawingSheetBuilder.SheetUnits.Metric;
            builder.MetricSheetTemplateLocation = template;
            builder.Name = "0" + _operationNumber + GetSheetName(drawingsFromat) + string.Format("{0:D3}", _currentSheetNumber);
            var nxObject = builder.Commit();
            nxObject.SetName(builder.Name);
            builder.Destroy();

            var sheet = nxObject as DrawingSheet;
            if (sheet == null) throw new Exception("Ошибка при создании нового листа!");
            return sheet;
        }

        /// <summary>
        /// Получает имя листа, в зависимости от типа формата
        /// </summary>
        /// <param name="nxDrawingsFromat"></param>
        /// <returns></returns>
        private string GetSheetName(NxDrawingsFromat nxDrawingsFromat)
        {
            switch (nxDrawingsFromat.DrawingType)
            {
                case 1:
                    return "_КАРТА_ИНСТРУМЕНТОВ_";
                case 2:
                    return "_КАРТА_НАЛАДКИ_";
                case 3:
                    return "_КАРТА_ЭСКИЗОВ_";
            }
            return "_ТИП_КАРТЫ_НЕ_ОПРЕДЕЛЕН!_";
        }

        /// <summary>
        /// Ищет указанные текстовые надписи и присваивает значения
        /// </summary>
        /// <param name="sheet"></param>
        private void SetSheetNotes(DrawingSheet sheet)
        {
            Note note;
            note =
                sheet.View.AskVisibleObjects()
                    .OfType<Note>()
                    .FirstOrDefault(n => n.GetText().Any(s => s.Contains("НОМЕР_ЛИСТА")));
            if (note != null) note.SetText(new[] { _currentSheetNumber.ToString() });


            note =
                sheet.View.AskVisibleObjects()
                    .OfType<Note>()
                    .FirstOrDefault(n => n.GetText().Any(s => s.Contains("НОМЕР_ОПЕРАЦИИ")));
            if (note != null) note.SetText(new[] { "0" + _operationNumber }); //OperationNumber
        }

        /// <summary>
        /// Ищет указанные текстовые надписи и присваивает значения для первой страницы
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="noteValue"></param>
        private void SetFirstSheetNotes(DrawingSheet sheet, int noteValue)
        {
            Note note;
            sheet.Open();
            note =
                sheet.View.AskVisibleObjects()
                    .OfType<Note>()
                    .FirstOrDefault(n => n.GetText().Any(s => s.Contains("ЛИСТОВ_ВСЕГО")));
            if (note != null) note.SetText(new[] { noteValue.ToString() });
        }
    }
}
