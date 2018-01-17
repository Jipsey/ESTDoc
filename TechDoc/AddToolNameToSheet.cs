using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NXOpen;
using NXOpen.Annotations;
using NXOpen.CAM;
using NXOpen.Drawings;
using NXOpen.UF;
using NXOpen.Utilities;

namespace NXClassLibrary1
{
    public class AddToolNameToSheet
    {
        static ListingWindow lw;
        static Session session;
        public static UFSession ufs;

        public static void Main(string[] args)
        {
            try
            {

                session = Session.GetSession();
                ufs = UFSession.GetUFSession();
                var ui = UI.GetUI();

                lw = session.ListingWindow;
                //                lw.Open();
                var part = session.Parts.Work;

                //if (!theSession.IsCamSessionInitialized()) theSession.CreateCamSession();

                bool answer;
                ufs.Cam.IsSessionInitialized(out answer);
                if (!answer) ufs.Cam.InitSession();
                var selectedTaggedObject = ui.SelectionManager.GetSelectedTaggedObject(0);
                if (selectedTaggedObject == null) throw new Exception("Не выбрана операция или группа операций!");

                var group = new NxOperationGroup(selectedTaggedObject.Tag, "<выбранный в диалоге станок>");
                var operationDescriptions = @group.GetOperationDescriptions();
                if (operationDescriptions == null) throw new ArgumentNullException("Не удалось получить информацию ни об одном инструменте!");

                int columnNums, rowNums, row = 0;
                var tableNote = GetTableInSheet(part, true);
                ufs.Tabnot.AskNmColumns(tableNote, out columnNums);
                ufs.Tabnot.AskNmRows(tableNote, out rowNums);

                foreach (string[] descr in operationDescriptions)
                {
                    Tag rowTag;
                    ufs.Tabnot.AskNthRow(tableNote, row++, out rowTag);
                    var col = 0;
                    foreach (var s in descr)
                    {
                        Tag colTag,cell;
                        ufs.Tabnot.AskNthColumn(tableNote, col++, out colTag);
                        ufs.Tabnot.AskCellAtRowCol(rowTag, colTag, out cell);
                        ufs.Tabnot.SetCellText(cell, s);
                        if(col > columnNums) break;
                    }
                    
                    if (row >= rowNums)
                    {
                        tableNote = GetTableInSheet(part, true);
                        ufs.Tabnot.AskNmColumns(tableNote, out columnNums);
                        ufs.Tabnot.AskNmRows(tableNote, out rowNums);
                        row = 0;
                    }
                }


//                for (var i = 0; i < 16; i++)
//                {
//                    Tag row, col, cell;
//                    ufs.Tabnot.AskNthRow(tableNote, i++, out row);
//                    ufs.Tabnot.AskNthColumn(tableNote, 3, out col);
//                    ufs.Tabnot.AskCellAtRowCol(row, col, out cell);
//                    ufs.Tabnot.SetCellText(cell, "Наименование сложной операции по обработке заготовки.Наименование сложной операции.");
//
//                    ufs.Tabnot.AskNthRow(tableNote, i++, out row);
//                    ufs.Tabnot.AskCellAtRowCol(row, col, out cell);
//                    ufs.Tabnot.SetCellText(cell, "Инструмент 1");
//
//                    ufs.Tabnot.AskNthRow(tableNote, i++, out row);
//                    ufs.Tabnot.AskCellAtRowCol(row, col, out cell);
//                    ufs.Tabnot.SetCellText(cell, "Инструмент 2");
//
//                    ufs.Tabnot.AskNthRow(tableNote, i, out row);
//                    ufs.Tabnot.AskCellAtRowCol(row, col, out cell);
//                    ufs.Tabnot.SetCellText(cell, "Инструмент 3");
//                }


                //                const double baseX = 50.5;
                //                const double baseY = 135.5;
                //                const double deltaY = 8.5;
                //                const double minY = 16.5;
                //
                //                const string filename = @"\\Data2\SAPR\UGS\NX 9.0\MACH\resource\tech_doc_plus\txt\карта_инструментов\фильтр_по_атрибутам.txt";
                //                var attributeFilter = GetAttributeFilter(filename);
                //                var positionX = baseX;
                //                var positionY = baseY;
                //
                //                var enumerator = part.DrawingSheets.GetEnumerator();
                //                enumerator.Reset();
                //                enumerator.MoveNext();
                //                
                //                foreach (Tool tool in tools)
                //                {
                //                    var sheet = (DrawingSheet) enumerator.Current;
                //                    if(!sheet.IsActiveForSketching) sheet.Open();
                //                    
                //                    var attributeCount = 0;
                //                    foreach (var attribute in tool.GetUserAttributes())
                //                    {
                //                        if(attributeFilter.Contains(attribute.Title)) continue;
                //                        attributeCount += 1;
                //                    }
                //                
                //                    var description = part.CAMSetup.CAMGroupCollection.CreateMillToolBuilder(tool).Description;
                //
                //                    var noteBuilder = part.Annotations.CreateDraftingNoteBuilder(null);
                //                    noteBuilder.Text.TextBlock.SetText(new []{description});
                //                    noteBuilder.Origin.Anchor = OriginBuilder.AlignmentPosition.BottomLeft;
                //                    noteBuilder.Origin.Plane.PlaneMethod = PlaneBuilder.PlaneMethodType.XyPlane;
                //                    noteBuilder.Origin.OriginPoint = new Point3d(positionX, positionY, 0.0);
                //                    noteBuilder.Commit();
                //                    noteBuilder.Destroy();
                //
                //                    positionY = positionY - deltaY * (4 + attributeCount);
                //
                //                    if (!(positionY <= minY)) continue;
                //
                //                    positionY = baseY;
                //                    enumerator.MoveNext();
                //                }
            }
            catch (Exception e)
            {
                UI.GetUI().NXMessageBox.Show("Exception", NXMessageBox.DialogType.Error, e.ToString());
            }
        }

        private static Tag GetTableInSheet(Part part, bool itsFirstSheet = true)
        {
            var drawingSheet = CreateSheet(part, itsFirstSheet);
            if (drawingSheet == null) throw new Exception("drawingSheet");

            foreach (var o in drawingSheet.View.AskVisibleObjects())
            {
                var tableSection = o as TableSection;
                if (tableSection == null || !tableSection.Name.Contains("КАРТА_ИНСТРУМЕНТОВ")) continue;

                Tag tableNote;
                ufs.Tabnot.AskTabularNoteOfSection(tableSection.Tag, out tableNote);

                if (tableNote != Tag.Null) return tableNote;
            }
            return Tag.Null;
        }

        private static DrawingSheet CreateSheet(Part part, bool itsFirstSheet)
        {
            var combine = Path.Combine(session.GetEnvironmentVariableValue("UGII_TEMPLATE_DIR"),"diakont_template_A4_GOST_3.1404-84_form_4.prt");
            if (!itsFirstSheet) combine = Path.Combine(session.GetEnvironmentVariableValue("UGII_TEMPLATE_DIR"), "diakont_template_A4_GOST_3.1404-84_form_4a.prt");

            DrawingSheet drawingSheet = null;
            var builder = part.DrawingSheets.DrawingSheetBuilder(drawingSheet);
            builder.Option = DrawingSheetBuilder.SheetOption.UseTemplate;
            builder.AutoStartViewCreation = false;
            builder.Units = DrawingSheetBuilder.SheetUnits.Metric;
            builder.MetricSheetTemplateLocation = combine;
            builder.Name = "000_КАРТА_ИНСТРУМЕНТОВ_" + builder.Number;
            builder.Commit();
            builder.Destroy();

            drawingSheet = part.DrawingSheets.CurrentDrawingSheet;
            drawingSheet.Open();
            return drawingSheet;
        }


        private static List<string> GetAttributeFilter(string filename)
        {
            var ret = new List<string>();
            if (File.Exists(filename)) ret.AddRange(File.ReadAllLines(filename));
            return ret;
        }
    }

    public class NxOperation
    {
        public int UF_PARAM_TL_NUM_FLUTES;
        public int UF_PARAM_CUTCOM_REGISTER_NUM;
        public int UF_PARAM_TL_NUMBER;
        public int UF_PARAM_TL_DIRECTION;
        public double UF_PARAM_TL_ZMOUNT;
        public double UF_PARAM_TL_FLUTE_LN;
        public double UF_PARAM_TL_DIAMETER;
        public double UF_PARAM_TL_TAPER_ANG;
        public double UF_PARAM_TL_POINT_ANG;
        public double UF_PARAM_TL_PITCH;
        public string UF_PARAM_TL_TEXT;
        public string UF_PARAM_TL_DESCRIPTION;
        public string UF_PARAM_TL_HOLDER_DESCRIPTION;
        private List<int> RequiredParams;
        public Tag tag;
        public UFSession ufs;
        public int OPERATION_TYPE;
        public Tag CUTTER_TAG;
        public int CUTTER_SUBTYPE;
        public int CUTTER_TYPE;
        public int UF_PARAM_TL_INSERTTYPE;
        public double UF_PARAM_TL_NOSE_RAD;
        public double UF_PARAM_TL_INSERT_WIDTH;
        public double UF_PARAM_TL_LEFT_ANG;
        public string UF_PARAM_TL_INSERTTYPE_STR;
        public double UF_PARAM_TL_TURN_HOLDER_ANGLE;
        public int UF_PARAM_TL_INSERT_POSITION;
        public NXObject.AttributeInformation[] CUTTER_ATTRIBUTES;

        public NxOperation(Tag tag)
        {
            if (AddToolNameToSheet.ufs == null) throw new Exception("Не удалось получить сессию пользовательских функций NX.");

            ufs = AddToolNameToSheet.ufs;
            this.tag = tag;

            GetParams();
            GetTypes();
            GetCutterInsertTypeStr();
            GetAttributes();
        }

        private void GetAttributes()
        {
            if (CUTTER_TAG == Tag.Null) return;

            var cutter = NXObjectManager.Get(CUTTER_TAG) as Tool;
            if (cutter == null) throw new Exception("NXObjectManager.Get(CUTTER_TAG) as Tool is NULL!");

            CUTTER_ATTRIBUTES = cutter.GetUserAttributes();
        }

        private void GetTypes()
        {
            if (ufs == null) return;
            ufs.Oper.AskOperType(tag, out OPERATION_TYPE);
            ufs.Oper.AskCutterGroup(tag, out CUTTER_TAG);
            ufs.Cutter.AskTypeAndSubtype(CUTTER_TAG, out CUTTER_TYPE, out CUTTER_SUBTYPE);
        }

        private void GetParams()
        {
            GetParamsIndxs();

            UF_PARAM_TL_DESCRIPTION = GetEncodeStrParams(UFConstants.UF_PARAM_TL_DESCRIPTION);
            UF_PARAM_TL_TEXT = GetStrParams(UFConstants.UF_PARAM_TL_TEXT);
            UF_PARAM_TL_HOLDER_DESCRIPTION = GetEncodeStrParams(UFConstants.UF_PARAM_TL_HOLDER_DESCRIPTION);
            UF_PARAM_TL_NUM_FLUTES = GetIntParams(UFConstants.UF_PARAM_TL_NUM_FLUTES);
            UF_PARAM_CUTCOM_REGISTER_NUM = GetIntParams(UFConstants.UF_PARAM_CUTCOM_REGISTER_NUM);
            UF_PARAM_TL_NUMBER = GetIntParams(UFConstants.UF_PARAM_TL_NUMBER);
            UF_PARAM_TL_DIRECTION = GetIntParams(UFConstants.UF_PARAM_TL_DIRECTION);
            UF_PARAM_TL_INSERTTYPE = GetIntParams(UFConstants.UF_PARAM_TL_INSERTTYPE);
            UF_PARAM_TL_ZMOUNT = GetDblParams(UFConstants.UF_PARAM_TL_ZMOUNT);
            UF_PARAM_TL_FLUTE_LN = GetDblParams(UFConstants.UF_PARAM_TL_FLUTE_LN);
            UF_PARAM_TL_DIAMETER = GetDblParams(UFConstants.UF_PARAM_TL_DIAMETER);
            UF_PARAM_TL_TAPER_ANG = GetDblParams(UFConstants.UF_PARAM_TL_TAPER_ANG);
            UF_PARAM_TL_POINT_ANG = GetDblParams(UFConstants.UF_PARAM_TL_POINT_ANG);
            UF_PARAM_TL_PITCH = GetDblParams(UFConstants.UF_PARAM_TL_PITCH);
            UF_PARAM_TL_NOSE_RAD = GetDblParams(UFConstants.UF_PARAM_TL_NOSE_RAD);
            UF_PARAM_TL_INSERT_WIDTH = GetDblParams(UFConstants.UF_PARAM_TL_INSERT_WIDTH);
            UF_PARAM_TL_LEFT_ANG = GetDblParams(UFConstants.UF_PARAM_TL_LEFT_ANG);
            UF_PARAM_TL_TURN_HOLDER_ANGLE = GetDblParams(UFConstants.UF_PARAM_TL_TURN_HOLDER_ANGLE);
            UF_PARAM_TL_INSERT_POSITION = GetIntParams(UFConstants.UF_PARAM_TL_INSERT_POSITION);

        }

        private string GetEncodeStrParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return string.Empty;

            string strValue;
            ufs.Param.AskStrValue(tag, paramIndex, out strValue);


            return EncodeStr(strValue);
        }

        private string GetStrParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return string.Empty;

            string strValue;
            ufs.Param.AskStrValue(tag, paramIndex, out strValue);
            return strValue;
        }

        private int GetIntParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return -1;
            int value;
            ufs.Param.AskIntValue(tag, paramIndex, out value);
            return value;
        }

        private double GetDblParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return -1;
            double value;
            ufs.Param.AskDoubleValue(tag, paramIndex, out value);
            return value;
        }

        private bool GetBoolParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return false;
            bool value;
            ufs.Param.AskLogicalValue(tag, paramIndex, out value);
            return value;
        }

        private void GetParamsIndxs()
        {
            if (ufs == null) return;
            int cnt;
            int[] indices;
            ufs.Param.AskRequiredParams(tag, out cnt, out indices);
            RequiredParams = new List<int>(indices);
        }

        private static string EncodeStr(string value)
        {
            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(value));
        }

        private void GetCutterInsertTypeStr()
        {
            if (ufs == null || CUTTER_TYPE != UFConstants.UF_CUTTER_TYPE_TURN || CUTTER_TYPE != UFConstants.UF_CUTTER_TYPE_GROOVE) return;

            switch (CUTTER_TYPE)
            {
                case UFConstants.UF_CUTTER_TYPE_TURN:
                    switch (UF_PARAM_TL_INSERTTYPE)
                    {
                        case UFConstants.UF_TURN_INSERTTYPE_PARALLEL_85: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - параллелограм 85<$s> ISO A "; return;
                        case UFConstants.UF_TURN_INSERTTYPE_PARALLEL_82: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - параллелограм 82<$s> ISO B "; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_80: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - ромб 80<$s> ISO C"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_100: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - ромб 100<$s> ISO C"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_55: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - ромб 55<$s> ISO D"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_75: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - ромб 75<$s> ISO E"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_HEXAGON: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - шестигр. ISO H"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_PARALLEL_55: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - параллелограм 55<$s> ISO K"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_RECTANGLE: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — прямоугольник ISO L"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_86: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — ромб 86<$s>  ISO M"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_OCTAGON: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — восьмиугольник ISO O"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_PENTAGON: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — пятиугольник ISO P"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_ROUND: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — круглая ISO R"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_SQUARE: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — квадрат ISO S"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_TRIANGLE: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — треугольная ISO T"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_35: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — ромб 35<$s>  ISO V"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_TRIGON: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — ломаный треугольник ISO W"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_USER: UF_PARAM_TL_INSERTTYPE_STR = "Вставка — произвольный профиль"; return;
                        default: UF_PARAM_TL_INSERTTYPE_STR = UF_PARAM_TL_INSERTTYPE.ToString(); return;
                    }
                case UFConstants.UF_CUTTER_TYPE_GROOVE:
                    switch (UF_PARAM_TL_INSERTTYPE)
                    {
                        case UFConstants.UF_TURN_INSERTTYPE_GRV_STD: UF_PARAM_TL_INSERTTYPE_STR = "Стандартный канавочный резец"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_GRV_FNR: UF_PARAM_TL_INSERTTYPE_STR = "Радиусный канавочный резец"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_GRV_RTJ: UF_PARAM_TL_INSERTTYPE_STR = "Круглый канавочный резец"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_GRV_USER: UF_PARAM_TL_INSERTTYPE_STR = "Канавочный резец произвольного профиля"; return;
                        default: UF_PARAM_TL_INSERTTYPE_STR = UF_PARAM_TL_INSERTTYPE.ToString(); return;
                    }
                default: return;
            }
        }
    }

    public class NxOperationGroup
    {
        public List<NxOperation> NxOperations;
        public string Name;
        public string Description;
        private List<string[]> OperationDescriptions;
        //        public List<OperationDescription> OperationDescriptions;


        public NxOperationGroup(Tag tag, string s)
            : this(tag)
        {
            Description = "Управляющая программа;  станок " + s;
        }

        public NxOperationGroup(Tag tag)
        {
            var ncGroup = NXObjectManager.Get(tag) as NCGroup;
            var operation = NXObjectManager.Get(tag) as Operation;
            if (ncGroup == null && operation == null)
                throw new ArgumentNullException("Не выбрана операция или группа операций!");

            if (ncGroup != null) GetOperations(ncGroup);
            if (operation != null) GetOperations(operation);

            OperationDescriptions = new List<string[]>();
        }

        private void GetOperations(NCGroup ncGroup)
        {
            Name = ncGroup.Name;
            NxOperations = new List<NxOperation>();
            foreach (var camObject in ncGroup.GetMembers())
                NxOperations.Add(new NxOperation(camObject.Tag));
        }

        private void GetOperations(Operation operation)
        {
            Name = operation.Name;
            NxOperations = new List<NxOperation> { new NxOperation(operation.Tag) };
        }

        private double RadiansToDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }
        
        private double DegreesToRadians(double degrees)
        {
            return degrees / (180 / Math.PI);
        }

        public List<string[]> GetOperationDescriptions()
        {
            var toolTags = new List<Tag>();

            foreach (var operation in NxOperations)
            {
                if (!toolTags.Contains(operation.CUTTER_TAG)) toolTags.Add(operation.CUTTER_TAG);
            }

            //            OperationDescriptions = new List<OperationDescription>();
            var strCount = 01;
            OperationDescriptions.Add(new[] { "У " + strCount++, "-", Name, Description, string.Empty, string.Empty, string.Empty });

            foreach (var toolTag in toolTags)
            {
                var list = NxOperations.FindAll(op => op.CUTTER_TAG == toolTag);
                var operation = list[0];

                //                OperationDescriptions.Add(new[] { "Тип инструмента ", operation.CUTTER_TYPE.ToString(), "Подтип инструмента ", operation.CUTTER_SUBTYPE.ToString() });
                OperationDescriptions.Add(new[] { "Т " + strCount++, "-", "Т " + operation.UF_PARAM_TL_NUMBER, operation.UF_PARAM_TL_DESCRIPTION, string.Empty, string.Empty, string.Empty });

                if (operation.CUTTER_TYPE != UFConstants.UF_CUTTER_TYPE_GROOVE && operation.CUTTER_TYPE != UFConstants.UF_CUTTER_TYPE_TURN)
                {
                    var last = OperationDescriptions.FindLast(str => str[0] != "");

                    var s = string.Format("<o>={0:N2}", operation.UF_PARAM_TL_DIAMETER);
                    if (Math.Abs(operation.UF_PARAM_TL_ZMOUNT) > 0) s += string.Format(";  вылет={0:N2}", operation.UF_PARAM_TL_ZMOUNT);
                    var s2 = "Н/А";
                    var find = list.Find(op => op.UF_PARAM_CUTCOM_REGISTER_NUM >= 0);
                    if (find != null) s2 = find.UF_PARAM_CUTCOM_REGISTER_NUM != 0 ? find.UF_PARAM_CUTCOM_REGISTER_NUM.ToString() : find.UF_PARAM_TL_NUMBER.ToString();
                    last[4] = s;
                    last[6] = s2;

                    s = string.Format("длина реж. кромки={0:N2}; кол. зубьев={1}", operation.UF_PARAM_TL_FLUTE_LN, operation.UF_PARAM_TL_NUM_FLUTES);
                    if (Math.Abs(operation.UF_PARAM_TL_TAPER_ANG) > 0)
                        s += string.Format(";  угол={0} <$s>",RadiansToDegrees(operation.UF_PARAM_TL_TAPER_ANG));
                    if(operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_DRILL)
                        s += string.Format("; угол заточки={0} <$s>", RadiansToDegrees(operation.UF_PARAM_TL_POINT_ANG));
                    OperationDescriptions.Add(new[] { "" + strCount++, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

                    if (Math.Abs(operation.UF_PARAM_TL_FLUTE_LN) > 0)
                        OperationDescriptions.Add(new[] { "" + strCount++, "-", string.Empty, string.Format("Длина рабочей части={0:N2} мм.", operation.UF_PARAM_TL_FLUTE_LN), string.Empty, string.Empty, string.Empty });

                    if (operation.UF_PARAM_TL_TEXT != string.Empty)
                        OperationDescriptions.Add(new[]{"" + strCount++, "-", string.Empty, operation.UF_PARAM_TL_TEXT, string.Empty, string.Empty,string.Empty});

                    if (operation.UF_PARAM_TL_HOLDER_DESCRIPTION != string.Empty)
                        OperationDescriptions.Add(new[]{"" + strCount++, "-", string.Empty, operation.UF_PARAM_TL_HOLDER_DESCRIPTION, string.Empty,string.Empty, string.Empty});
                }
                else if (operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_GROOVE || operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_TURN)
                {
                    var last = OperationDescriptions.FindLast(str => str[0] != "");
                    
                    switch (operation.CUTTER_TYPE)
                    {
                        case UFConstants.UF_CUTTER_TYPE_TURN: last[4] = string.Format("Rверш.={0:N2}", operation.UF_PARAM_TL_NOSE_RAD); break;
                        case UFConstants.UF_CUTTER_TYPE_GROOVE: last[4] = string.Format("Ширина={0:N2}", operation.UF_PARAM_TL_INSERT_WIDTH); break;
                        //TODO ДОБАВИТЬ ТИП резьбовых резцов 
                        //last[4] = "Угол пофиля="+operation.UF_PARAM_TL_LEFT_ANG+"<$s>";
                    }

                    var s = operation.UF_PARAM_TL_INSERTTYPE_STR;
                    OperationDescriptions.Add(new[]{"" + strCount++, "-", string.Empty, s, string.Empty,string.Empty, string.Empty});

                    s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
                    OperationDescriptions.Add(new[]{"" + strCount++, "-", string.Empty, s, string.Empty,string.Empty, string.Empty});

                    s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция втавки — верхняя сторона": "Позиция втавки — нижняя сторона";
                    OperationDescriptions.Add(new[] { "" + strCount++, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
                }
                
                foreach (var attr in operation.CUTTER_ATTRIBUTES)
                {
                    var s = attr.Title +": "+attr.StringValue;
                    if (attr.Title.Contains("ID_TOOL")) s = "Код MAX инструмента = " + attr.StringValue;
                    if (attr.Title.Contains("ID_INSERT")) s = "Код MAX реж. вставки = " + attr.StringValue;
                    OperationDescriptions.Add(new[] { "" + strCount++, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
                }
            }
            return OperationDescriptions;
        }
    }
}
