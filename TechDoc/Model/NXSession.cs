using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NXOpen;
using NXOpen.Assemblies;
using NXOpen.CAM;
using NXOpen.Tooling;
using NXOpen.UF;
using NXOpen.Utilities;
using TechDocNS.Services;

namespace TechDocNS.Model
{
    public class NxSession
    {
        public static Session Session;
        public static UFSession Ufs;
        public static Part Part;
        public NxAttribute Attributes;
        public NxAdditionalData Additional;
        public NxDrawingCreator Creator;
        public static string UGII_CAM_RESOURCE_DIR;
        public static string ROOT_PATH;
        public static string ROOT_PATH_TXT;
        public static string UGII_TEMPLATE_DIR;
        public static string UGII_CUSTOM_DIRECTORY_FILE;
        public string SETUP_CARD_FILE_TT;
        public string SKETCH_CARD_FILE_TT;
        public List<TaggedObject> TaggedObjects; // { get { return GetSelectedObjects(); }}
        public IEnumerable<NxOperationGroup> NxOperationGroups;


        //------------------------------------------
//        public static NXOpen.CAM.Operation TempOperation;
//        public static NXOpen.CAM.CAMObject[] arrCAMObj;
        
        //------------------------------------------

        public NxSession(Session theSession, UI theUi)
        {
            if (theSession == null) throw new Exception("Сессия NX не запущена!");
            Session = theSession;

            Part = Session.Parts.Display;
            if (Part == null) throw new Exception("Не найдена открытая деталь!");

            Ufs = UFSession.GetUFSession();
            if (Ufs == null) throw new Exception("GetUFSession!");

            NxLogger.InitializeLogger(theSession);
            NxLogger.ToListingWindow = true;

            ROOT_PATH = GetRootDirectory();

            var directories = Directory.GetDirectories(ROOT_PATH, "txt", SearchOption.AllDirectories);
            if (!directories.Any()) throw new Exception("Не найдена корневая директория с тектовыми файлами программы!");
            ROOT_PATH_TXT = directories.First();

            UGII_TEMPLATE_DIR = Session.GetEnvironmentVariableValue("UGII_TEMPLATE_DIR");

            TaggedObjects = new List<TaggedObject>();

            InitializeToolsCollection();
        }

        private void InitializeToolsCollection()
        {
            int type, subtype;
            var ncGroups = Part.CAMSetup.CAMGroupCollection.ToArray();
            var toolsGroups = ncGroups.Where(gr =>
            {
                Ufs.Obj.AskTypeAndSubtype(gr.Tag, out type, out subtype);
                return type == UFConstants.UF_machining_mach_tool_grp_type;
            })
                .Select(
                    g =>
                    {
                        Ufs.Obj.AskTypeAndSubtype(g.Tag, out type, out subtype);
                        return new ToolsCollection
                        {
                            Tag = g.Tag,
                            Name = g.Name,
                            Members = GetMembers(g.GetMembers()).OfType<Tool>().Select(t => t.Tag),
                            IsRevolver = subtype == 1,
                            IsHole = subtype == 2
                        };
                    }).ToList();

            RevolversToolsCollection = toolsGroups.Where(gr => gr.IsRevolver);
            HolesToolsCollection = toolsGroups.Where(gr => gr.IsHole);

            //            var lw = Session.ListingWindow;
            //            lw.Open();
            //            foreach (var ncGroup in ncGroups)
            //            {
            //                Ufs.Obj.AskTypeAndSubtype(ncGroup.Tag, out type, out subtype);
            //                if (type == UFConstants.UF_machining_mach_tool_grp_type && subtype == 1) lw.WriteLine("\t\t Revolver!");
            //                lw.WriteLine(String.Format("Object: {0} | Tag: {4} | Name: {3} | Type: {1} | Subtype: {2}", ncGroup.GetType(),
            //                    type, subtype, ncGroup.Name, ncGroup.Tag));
            //            }
        }

        private static IEnumerable<ToolsCollection> HolesToolsCollection { get; set; }

        public static IEnumerable<ToolsCollection> RevolversToolsCollection { get; private set; }

        private static IEnumerable<CAMObject> GetMembers(IEnumerable<CAMObject> list)
        {
            return list.Concat(list.OfType<NCGroup>()
                .SelectMany(gr => GetMembers(gr.GetMembers())));
        }

        private static string GetRootDirectory()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var directoryInfo = new DirectoryInfo(directory).Parent;
            return directoryInfo != null ? directoryInfo.FullName : directory;

            //            UGII_CAM_RESOURCE_DIR = GetVariableValue("UGII_CAM_RESOURCE_DIR");
            //            if (string.IsNullOrEmpty(UGII_CAM_RESOURCE_DIR)) throw new Exception("Не задана переменная окружения UGII_CAM_RESOURCE_DIR!");
            //
            //                       UGII_CUSTOM_DIRECTORY_FILE = GetVariableValue("UGII_CUSTOM_DIRECTORY_FILE");
            //                        if (string.IsNullOrEmpty(UGII_CAM_RESOURCE_DIR)) throw new Exception("Не задана переменная окружения UGII_CUSTOM_DIRECTORY_FILE!");
            //                        if (!File.Exists(UGII_CUSTOM_DIRECTORY_FILE))
            //
            //            var directories = Directory.GetDirectories(UGII_CAM_RESOURCE_DIR, "tech_doc*", SearchOption.AllDirectories);
            //            if (!directories.Any()) throw new Exception("Не найдена корневая директория программы!");

        }

        public void InitializeAttribute()
        {
            if (Part == null) throw new Exception("Не найдена открытая деталь!");
            Attributes = new NxAttribute(this);
            Attributes.GetAttributeFromFiles();
        }

        public void InitializeDrawingsData()
        {
            if (Part == null) throw new Exception("Не найдена открытая деталь!");

            Additional = new NxAdditionalData(this);
            Additional.InitializeData();
            GetSelectedObjects();
        }


        public string GetVariableValue(string variableName)
        {
            return Session == null ? string.Empty : Session.GetEnvironmentVariableValue(variableName);
        }



        //                // Get the selected nodes from the Operation Navigator
        //                int selectedCount;
        //                Tag[] selectedTags;
        //                ufs.UiOnt.AskSelectedNodes(out selectedCount, out selectedTags);
        //                ui.NXMessageBox.Show("ui.SelectionManager.GetSelectedTaggedObject(0): ", NXMessageBox.DialogType.Information, selectedCount.ToString());
        //                NXOpen.Utilities.NXObjectManager.Get(selectedTags[0]);
        public void GetSelectedObjects()
        {
            // Переключимся в режим выбора программ
            try
            {
                Ufs.Cam.InitSession();
                Ufs.UiOnt.SwitchView(UFUiOnt.TreeMode.Order);

                // Get the selected nodes from the Operation Navigator
                int selectedCount;
                Tag[] selectedTags;
                if (Ufs == null) return;
                Ufs.UiOnt.AskSelectedNodes(out selectedCount, out selectedTags);
                var taggedObjects = selectedTags.Select(NXObjectManager.Get).ToList();
                var objects = taggedObjects.Where(obj => obj is NCGroup || obj is NXOpen.CAM.Operation).ToList();
                TaggedObjects = objects;
            }
            catch (Exception e)
            {
                UI.GetUI()
                    .NXMessageBox.Show("Не активирован режим Обработка", NXMessageBox.DialogType.Error,
                        "Для работы программы необходимо активировать режим обработка и обновить диалог!");
            }
        }

        public void GetNxOperationGroups()
        {
            if (TaggedObjects == null)
                throw new Exception("Не удалось получить выделенные объекты из навигатора операций!");

            var groups = TaggedObjects.OfType<NCGroup>().ToList();
            var operations = TaggedObjects.OfType<NXOpen.CAM.Operation>().ToList();

            if (!groups.Any() && !operations.Any())
                throw new Exception("Ошибка вызова метода создания листа. Выбраный объект не является операцией или группой");

            // сначала получим все родительские выделенные группы операций
            var ncGroups = groups
                .Where(gr =>
                    !groups.Any(g =>
                        g.GetMembers().Contains(gr)));

            // если ни одна из групп операций не содержит выделенную операцию, добавим ее к группам
            var taggedObjects = ncGroups
                .Concat(operations
                .Where(op =>
                    !groups.Any(gr => gr.GetMembers().Contains(op)))
                .Cast<TaggedObject>());
            
            var additionalToolName = Additional.SelectedMachine;
            var operationGroups = taggedObjects
                .Select(o => new NxOperationGroup(o, additionalToolName));
            NxOperationGroups = operationGroups;
        }
        
        public List<string> GetHelpList(string fileName)
        {
            return Directory.GetFiles(System.IO.Path.Combine(ROOT_PATH_TXT, "справка"))
                    .Where(f => System.IO.Path.GetFileNameWithoutExtension(f) == fileName)
                    .SelectMany(f => File.ReadAllLines(f, Encoding.Default))
                    .ToList();
        }

        public static string GetDirectory(string dirName)
        {
            return Directory.GetDirectories(ROOT_PATH_TXT)
                .FirstOrDefault(d => d.Contains(dirName));
        }

        public static List<string> GetToolsCardAttributesFilter()
        {
            var tools_card = GetDirectory("карта_инструментов");
            return !string.IsNullOrEmpty(tools_card)
                ? (Directory.GetFiles(tools_card, "фильтр_по_атрибутам.txt", SearchOption.AllDirectories))
                .SelectMany(f => File.ReadAllLines(f, Encoding.Default))
                .Where(l => !l.StartsWith("!")).ToList()
                : null;
        }

        /// <summary>
        /// только для элкетроискровых станков
        /// </summary>
        /// <returns></returns>
        public static List<string> GetSparkToolGroupNotes()
        {
            var firstOrDefault =
                Directory.GetDirectories(ROOT_PATH_TXT, "электроискровая", SearchOption.AllDirectories).FirstOrDefault();
            return !string.IsNullOrEmpty(firstOrDefault)
                ? (Directory.GetFiles(firstOrDefault, "проволока.txt", SearchOption.AllDirectories))
                .SelectMany(f => File.ReadAllLines(f, Encoding.Default))
                .Where(l => !l.StartsWith("!")).ToList()
                : null;
        }

        public void CreateDrawings(List<DrawingsSetup> drawingsSetups)
        {
            for (var index = 0; index < drawingsSetups.Count; index++)
            {
                var drawingsSetup = drawingsSetups[index];
                
                drawingsSetup.DrawingsFormats = Additional.DrawingsFromats
                    .Where(df => df.DrawingType == drawingsSetup.DrawingsType && df.Name == drawingsSetup.DrawingsFormatName).ToArray();
                
                if (!drawingsSetup.DrawingsFormats.Any() && drawingsSetup.DrawingsType != 0)
                    throw new Exception(string.Format("Не найдены форматки для создания листа карты типа {0} и формата {1}", drawingsSetup.DrawingsType, drawingsSetup.DrawingsFormatName));

                if (drawingsSetup.DrawingsType == 0)
                {
                    if (drawingsSetup.AdditionalToolGroupName.Contains("электроискровая"))
                        drawingsSetup.SparkToolGroupNotes = GetSparkToolGroupNotes();
                }
                if (drawingsSetup.DrawingsType == 1)
                {
                    //drawingsSetup.AdditionalTools = TaggedObjects; //TaggedObjects.First();
                    drawingsSetup.NxOperationGroups = NxOperationGroups;
                }
                else if (drawingsSetup.DrawingsType == 2)
                {
                    if (!string.IsNullOrEmpty(SETUP_CARD_FILE_TT)) drawingsSetup.AdditionalFile = SETUP_CARD_FILE_TT;
                }
                else if (drawingsSetup.DrawingsType == 3)
                {
                    if (!string.IsNullOrEmpty(SKETCH_CARD_FILE_TT)) drawingsSetup.AdditionalFile = SKETCH_CARD_FILE_TT;
                }
                drawingsSetups[index] = drawingsSetup;
            }

            Creator = new NxDrawingCreator(this);
            Creator.CreateDrawnings(drawingsSetups);
#if DEBUG
     //       GetOperationAttributes(TaggedObjects);
#endif
        }

        private void GetOperationAttributes(List<TaggedObject> taggedObjects)
        {
            var group = new NxOperationGroup(taggedObjects, "<null>");
            var operations = group.NxOperations.GroupBy(op => op.CUTTER_TAG).Select(gr => gr.First()).ToList();

            var type = typeof(UFConstants);
            var fields = type.GetFields()
                .Select(f => new { Name = f.Name, Value = f.GetValue(null), FieldType = f.FieldType }).ToList();

//            var list = new List<string>();
            var lw = Session.ListingWindow;
            lw.Open();
            foreach (var operation in operations)
            {
                lw.WriteFullline(new string('=', 80));
                lw.WriteFullline(operation.UF_PARAM_TL_DESCRIPTION);

                var operationFields = operation.RequiredParams
                    .Where(p => fields.Where(f => f.Value is int).Any(f => (int)f.Value == p))
                    .Select(p => fields.Where(f => f.Value is int).FirstOrDefault(f => (int)f.Value == p))
                    .Where(f => f != null).ToList();

                foreach (var operationField in operationFields)
                {
                    var val = "";
                    try
                    {
                        val = operation.GetStrParams((int)operationField.Value);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            val = operation.GetDblParams((int)operationField.Value).ToString();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                val = operation.GetIntParams((int)operationField.Value).ToString();
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(val)) continue;

                    //var s = string.Format("Parameter: {0} \t\t\t| Index: {1} \t\t\t| Value: {2}", operationField.Name, operationField.Value, val);
                    var s = string.Format("Parameter: {0, -50} | Index: {1, -10} | Value: {2}", operationField.Name, operationField.Value, val);
                    lw.WriteFullline(s);
                }
            }

            //list.ForEach(s => Session.ListingWindow.WriteLine(s));
        }
    }

    public static class NxLogger
    {
        private static ListingWindow _lw;
        public static bool ToListingWindow { get; set; }
        public static bool ToMessageBox { get; set; }

        public static void InitializeLogger(Session session)
        {
            if (session == null) return;
            _lw = session.ListingWindow;
        }

        public static void Log(string s)
        {
            if(_lw == null || s == null) return;

            if (ToListingWindow) LogToListingWindow(s);
            if (ToMessageBox) LogToMessageBox(s);
        }

        private static void LogToMessageBox(string s)
        {
            return;
        }

        private static void LogToListingWindow(string s)
        {
            if (!_lw.IsOpen) _lw.Open();
            _lw.WriteFullline(s);
        }
    }

    public class ToolsCollection
    {
        public Tag Tag { get; set; }
        public string Name { get; set; }
        public IEnumerable<Tag> Members { get; set; }
        public bool IsRevolver { get; set; }
        public bool IsHole { get; set; }
    }
}
