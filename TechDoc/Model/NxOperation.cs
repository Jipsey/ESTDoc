using NXOpen;
using NXOpen.CAM;
using NXOpen.UF;
using NXOpen.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TechDocNS.Services;


namespace TechDocNS.Model
{
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
        public string UF_PARAM_TL_INSERTTYPE_STR;
        public string UF_TYPE_OPERATION_NAME; // тип операции
        public List<int> RequiredParams;
//        public Tag OperationTag;
        public UFSession ufs;
        public int OPERATION_TYPE;
        public Tag CUTTER_TAG;
        public int CUTTER_SUBTYPE;
        public int CUTTER_TYPE;
        public int UF_PARAM_TL_INSERTTYPE;
        public double UF_PARAM_TL_NOSE_RAD;
        public double UF_PARAM_TL_INSERT_WIDTH;
        public double UF_PARAM_TL_LEFT_ANG;
        public double UF_PARAM_TL_TURN_HOLDER_ANGLE;
        public int UF_PARAM_TL_INSERT_POSITION;
        public NXObject.AttributeInformation[] CUTTER_ATTRIBUTES;
        public double UF_PARAM_TL_COR1_RAD;
        public double UF_PARAM_TL_TIP_ANG;
        public double UF_PARAM_TL_SHANK_DIA;
        public double UF_PARAM_TL_BARREL_RAD;
        public double UF_PARAM_TL_LOW_COR_RAD;
        public double UF_PARAM_TL_UP_COR_RAD;
        public double UF_PARAM_TL_BUTTON_DIAMETER;
        public double UF_PARAM_TL_LEFT_COR_RAD;
        public double UF_PARAM_TL_RADIUS;
        public double UF_PARAM_TL_POINT_ANG_COUNTERSINK;
        public int UF_PARAM_TL_HOLDER_NUMBER;
        public readonly NXOpen.CAM.Operation Operation;
        private readonly NxOperationGroup _operationGroup;
        public Tool Tool;
        public bool UF_PARAM_TL_CutComCorrection; // перменная хранящся булево значение о включённой/выключенной коррекции
        public bool UF_PARAM_TL_CutcomOutputContactPoint;
        public bool UF_PARAM_TL_FlipToolAroundHolder;
        public int UF_MCS_NUMBER;       
        public double UF_PARAM_TL_MaxReach;   // значение зоны досягаемости для токарных расточных иснтрументов
        public int UF_TL_CutcomRegister; // номер корректора
        public static Dictionary<Tag, List<string>> toolListRegister; // мапа для хранения параметров коррекции инструмента 
        
        private string _toolNumber;
        public string ToolNumber
        {
            get { return _toolNumber ?? (_toolNumber = GetToolNumber()); }
        }

        public Dictionary<Tag, List<string>> getToolListRegister()
        {
            if (toolListRegister == null)
                return null;
            else return toolListRegister;
        }
        public NxOperation(NXOpen.CAM.Operation operation, NxOperationGroup nxOperationGroup)
        {
            if (operation == null)
                throw new Exception("Получение операций. Объект не является типом операция.");

            Operation = operation;

            //String n = Operation.Name;
            System.Type t = Operation.GetType();
            
            UF_TYPE_OPERATION_NAME = t.Name;// if SurfaceContour

            _operationGroup = nxOperationGroup;
            
            ufs = NxSession.Ufs;
            if (ufs == null) 
                throw new Exception("Не удалось получить сессию пользовательских функций NX.");
 
            GetParams();
            // GetCutterTool(); <--перенёс в метод GetParams()
            GetCutterInsertTypeStr();
            GetChanellNumber();
        }

        private void GetChanellNumber()
        {
            if (_operationGroup == null) return;
            if (_operationGroup.MachineName != null && !_operationGroup.MachineName.Contains("CTX beta 1250 TC 4A")) return;

            var parentGroupName = string.Empty;
            if (NxSession.RevolversToolsCollection != null)
            {
                var toolsCollection = NxSession.RevolversToolsCollection.FirstOrDefault(gr => gr.Members.Contains(CUTTER_TAG));
                if (toolsCollection != null) parentGroupName = toolsCollection.Name;
            }

            ChanellNumber = parentGroupName.StartsWith("1")
                ? 1
                : (parentGroupName.StartsWith("2")
                    ? 2
                    : 0);
        }

        private void GetCutterTool()
        {
            if (ufs == null) return;
            ufs.Oper.AskOperType(Operation.Tag, out OPERATION_TYPE);

            ufs.Oper.AskCutterGroup(Operation.Tag, out CUTTER_TAG);
            ufs.Cutter.AskTypeAndSubtype(CUTTER_TAG, out CUTTER_TYPE, out CUTTER_SUBTYPE);
            if (CUTTER_TAG == Tag.Null) throw new Exception("Не удалось получить инструмент из операции!");
            
 
            Tool = NXObjectManager.Get(CUTTER_TAG) as Tool;
            if (Tool == null) throw new Exception("Не удалось получить инструмент из операции!");

            //----------------определяем тип инструмента----------------------------
            Tool.Types tt; //Tool.Types.MillForm;
            Tool.Subtypes ts;  // Tool.Subtypes.Undefined;
           
            Tool.GetTypeAndSubtype(out tt, out ts);
            if (tt == Tool.Types.MillForm)
               {
                 UF_PARAM_TL_DIAMETER = determinateDiameterOfUserTool();
               }
            //--------------------------------------------

            CUTTER_ATTRIBUTES = Tool.GetUserAttributes();
        }


        private void GetParams()
        {
            GetRequiredParams();
#if DEBUG
            //-----------------------
            // makeMap();
            //-----------------------
#endif
            UF_PARAM_TL_DESCRIPTION = enc(GetStrParams(UFConstants.UF_PARAM_TL_DESCRIPTION));
            UF_PARAM_TL_TEXT = enc (GetStrParams(UFConstants.UF_PARAM_TL_TEXT));     
            UF_PARAM_TL_HOLDER_DESCRIPTION = enc(GetStrParams(UFConstants.UF_PARAM_TL_HOLDER_DESCRIPTION));//GetEncodeStrParams(UFConstants.UF_PARAM_TL_HOLDER_DESCRIPTION);
            UF_PARAM_TL_NUM_FLUTES = GetIntParams(UFConstants.UF_PARAM_TL_NUM_FLUTES);
            UF_PARAM_CUTCOM_REGISTER_NUM = GetIntParams(UFConstants.UF_PARAM_CUTCOM_REGISTER_NUM);
            UF_PARAM_TL_NUMBER = GetIntParams(UFConstants.UF_PARAM_TL_NUMBER);
            UF_PARAM_TL_DIRECTION = GetIntParams(UFConstants.UF_PARAM_TL_DIRECTION);
            UF_PARAM_TL_INSERTTYPE = GetIntParams(UFConstants.UF_PARAM_TL_INSERTTYPE);
            UF_PARAM_TL_ZMOUNT = GetDblParams(UFConstants.UF_PARAM_TL_ZMOUNT);
            UF_PARAM_TL_FLUTE_LN = GetDblParams(UFConstants.UF_PARAM_TL_FLUTE_LN);
            UF_PARAM_TL_DIAMETER = GetDblParams(UFConstants.UF_PARAM_TL_DIAMETER);

            //------------------------------------------------
            //UF_PARAM_TL_TURN_HOLDER_ANGLE = GetCutterAngleOrient();                                         <-- перенёс после метода GetCutterTool();
            //------------------------------------------------
            UF_TL_CutcomRegister = GetIntParams(UFConstants.UF_PARAM_TL_CUTCOM_REG); // определяем номер корректора
            UF_MCS_NUMBER = GetIntParams(UFConstants.UF_APP_PUNCH); // определяем номер СКС точки
            //------------------------------------------------
            UF_PARAM_TL_TAPER_ANG = GetDblParams(UFConstants.UF_PARAM_TL_TAPER_ANG);
            UF_PARAM_TL_POINT_ANG_COUNTERSINK = GetDblParams(1024);
            UF_PARAM_TL_POINT_ANG = GetDblParams(UFConstants.UF_PARAM_TL_POINT_ANG);
            UF_PARAM_TL_PITCH = GetDblParams(UFConstants.UF_PARAM_TL_PITCH);
            UF_PARAM_TL_NOSE_RAD = GetDblParams(UFConstants.UF_PARAM_TL_NOSE_RAD);
            UF_PARAM_TL_INSERT_WIDTH = GetDblParams(UFConstants.UF_PARAM_TL_INSERT_WIDTH);
            UF_PARAM_TL_LEFT_ANG = GetDblParams(UFConstants.UF_PARAM_TL_LEFT_ANG);          
            UF_PARAM_TL_INSERT_POSITION = GetIntParams(UFConstants.UF_PARAM_TL_INSERT_POSITION);
            UF_PARAM_TL_COR1_RAD = GetDblParams(UFConstants.UF_PARAM_TL_COR1_RAD);
            UF_PARAM_TL_TIP_ANG = GetDblParams(UFConstants.UF_PARAM_TL_TIP_ANG);
            UF_PARAM_TL_SHANK_DIA = GetDblParams(UFConstants.UF_PARAM_TL_SHANK_DIA);
            UF_PARAM_TL_BARREL_RAD = GetDblParams(UFConstants.UF_PARAM_TL_BARREL_RAD);
            UF_PARAM_TL_LOW_COR_RAD = GetDblParams(UFConstants.UF_PARAM_TL_LOW_COR_RAD);
            UF_PARAM_TL_UP_COR_RAD = GetDblParams(UFConstants.UF_PARAM_TL_UP_COR_RAD);
            UF_PARAM_TL_BUTTON_DIAMETER = GetDblParams(UFConstants.UF_PARAM_TL_BUTTON_DIAMETER);
            UF_PARAM_TL_LEFT_COR_RAD = GetDblParams(UFConstants.UF_PARAM_TL_LEFT_COR_RAD);
            UF_PARAM_TL_RADIUS = GetDblParams(UFConstants.UF_PARAM_TL_RADIUS);
            UF_PARAM_TL_HOLDER_NUMBER = GetIntParams(UFConstants.UF_PARAM_TL_HOLDER_NUMBER);

            GetCutterTool();

            if (CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_TURN || CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_GROOVE 
                || CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_THREAD)
            {
                if (CUTTER_TYPE != UFConstants.UF_CUTTER_TYPE_THREAD)
                {
                    UF_PARAM_TL_TURN_HOLDER_ANGLE = GetCutterAngleOrientAndFlipToolAroundHolder();
                }
                buildToolListRegister();
                UF_PARAM_TL_MaxReach = GetTL_MaxReach(); // получаем значение зоны досягаемости 
                
            }
        }
        // получаем параметры коррекции
        public bool getCutComParams(){

            NXOpen.CAM.VolumeBased25DMillingOperationBuilder operationBuilder;
            NXOpen.CAM.NcmPlanarBuilder.CutcomTypes cutCom;
            NXOpen.CAM.NcmHoleMachining.CutcomTypes cutComHoleMilling;
            NXOpen.CAM.CylinderMillingBuilder cylinderMillingBuilder;

            try
            {
                operationBuilder = NxSession.Part.CAMSetup.CAMOperationCollection.CreateVolumeBased25dMillingOperationBuilder(Operation);
                cutCom = operationBuilder.NonCuttingBuilder.CutcomType;
                UF_PARAM_TL_CutcomOutputContactPoint = operationBuilder.NonCuttingBuilder.CutcomOutputContactPoint;
                operationBuilder.Destroy();
                return  cutCom.Equals(NXOpen.CAM.NcmPlanarBuilder.CutcomTypes.None) ? false : true;
            }
            catch {
                try
                {
                    cylinderMillingBuilder = NxSession.Part.CAMSetup.CAMOperationCollection.CreateCylinderMillingBuilder(Operation);
                    cutComHoleMilling = cylinderMillingBuilder.NonCuttingBuilder.CutcomType;
                    UF_PARAM_TL_CutcomOutputContactPoint = cylinderMillingBuilder.NonCuttingBuilder.CutcomOutputContactPoint;
                    cylinderMillingBuilder.Destroy();
                    return cutComHoleMilling.Equals(NXOpen.CAM.NcmHoleMachining.CutcomTypes.None) ? false : true;
                }
                catch { 
                    return false; 
                   }
            }

        }
        

        //создаём коллекцию номеров регистра коррекции
        private void buildToolListRegister() {

            if (toolListRegister == null)
            {
                toolListRegister = new Dictionary<Tag, List<string>>();
            }

            var toolTag = CUTTER_TAG;
            StringBuilder sb = new StringBuilder(30);

            string s = UF_PARAM_TL_FlipToolAroundHolder == false ? "стандартный" : "обратный";
                      //UF_MCS_NUMBER <= 1 ? " левый" : " правый");

            object[] toolParams = { UF_TL_CutcomRegister, s, UF_PARAM_TL_TURN_HOLDER_ANGLE.ToString() };


            if (!toolListRegister.ContainsKey(toolTag))
            {
                toolListRegister.Add(toolTag, new List<string>());
            }
            sb.AppendFormat("Корректор №{0}: {1}, угол ориентации резца {2}<$s>", toolParams); // создаём описание корректора  // °
            string toolReg = sb.ToString();
            if (!toolListRegister[toolTag].Contains(toolReg))
                toolListRegister[toolTag].Add(toolReg); // добавляем в лист по номеру инструмента описание корректора

        }

        private string GetEncodeStrParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return string.Empty;

            string strValue;
            ufs.Param.AskStrValue(Operation.Tag, paramIndex, out strValue);

            return EncodeStr(strValue);
        }

        public string GetStrParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return string.Empty;

            string strValue;
            ufs.Param.AskStrValue(Operation.Tag, paramIndex, out strValue);
            return strValue;
        }

        public int GetIntParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return -1;
            int value;
            ufs.Param.AskIntValue(Operation.Tag, paramIndex, out value);
            return value;
        }

        public double GetDblParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || !RequiredParams.Contains(paramIndex)) return -1;
            double value = 0.0;

            if (UF_TYPE_OPERATION_NAME.Equals("SurfaceContour") && paramIndex == 1000)
            {
                string nameTool = determinateNameOfTool();  // получаем имя инструмента
                value = determinateDiameterOfTool(nameTool);
            }
            else
            {
                ufs.Param.AskDoubleValue(Operation.Tag, paramIndex, out value);//1082 1002 1000
            }

            var round = Math.Round(value, 2);
            return round;
        }

        public double determinateDiameterOfUserTool() {

            if (Operation == null)
                return 0.0;
            //создаем инструмент
            Tag tempCutterTag;
            ufs.Oper.AskCutterGroup(Operation.Tag, out tempCutterTag);
            Tool tTool = NXObjectManager.Get(tempCutterTag) as Tool;
            string nameTrPoint;
            int DefType;
            double doubleDiameter;
            double doubleDistance;
            double zOffset;
            int zOffsetUsed;
            int adjust;
            int adjustUsed;
            int cutcom;
            int cutcomUsed;

            NXOpen.CAM.MillFormToolBuilder millFormToolBuilder;
            millFormToolBuilder = NxSession.Part.CAMSetup.CAMGroupCollection.CreateMillFormToolBuilder(tTool);

            NXObject nxobj = millFormToolBuilder.MillingTrackpointBuilder.GetTrackPoint(0);
            millFormToolBuilder.MillingTrackpointBuilder.GetTrackPoint(nxobj, out nameTrPoint, out DefType, out doubleDiameter, out doubleDistance, out zOffset, 
                out zOffsetUsed, out adjust, out adjustUsed,
                out cutcom, out cutcomUsed);
            
            millFormToolBuilder.Destroy();

            return Math.Round( doubleDiameter,2);
        }
#if DEBUG

        public void makeMap()
        {
                    Dictionary<int, string> map = new Dictionary<int,string>();
                    Dictionary<double, string> doubleValueMap = new Dictionary<double, string>();
                    Dictionary<double, string> doubleMap = new Dictionary<double, string>(); 
                    Dictionary<int, string> logicalMap = new Dictionary<int,string>();

                 int i;
                 int cnt =0;
                 int cnt2 = 0;
                 int cnt3 = 0;
                 double doubleValue;
                 bool logical;

                 foreach (int element in RequiredParams)
                 {
                     UFParam.IndexAttribute attribute;
                     ufs.Param.AskParamAttributes(element, out attribute);
                     string name = attribute.name;
                     UFParam.Type type = attribute.type;
                     
                     if (attribute.type == UFParam.Type.TypeInt){
                         
                         ufs.Param.AskIntValue(Operation.Tag, element, out i);
                         map.Add(cnt++, " " + element + "  " + enc(name) + " TYPE IS " + type + " " + i);
                     }
                     if(attribute.type == UFParam.Type.TypeDouble){
                         ufs.Param.AskDoubleValue(Operation.Tag, element, out doubleValue);
                         doubleValueMap.Add(cnt2++, " " + element + "  " + enc(name) + " TYPE IS " + type + " " + doubleValue);
                         }
                     if (attribute.type == UFParam.Type.TypeLogical) {
                         ufs.Param.AskLogicalValue(Operation.Tag,element, out logical);
                         logicalMap.Add(cnt3++, " " + element + "  " + enc(name) + " TYPE IS " + type + " " + logical);
                     }
                 
                 }
                     
                     writeFile(map);
                     writeDoubleValue(doubleValueMap);
                     writeBooleanValue(logicalMap);         
        }

        public void writeFile(Dictionary<int, string> map)
        {

            FileStream file = new FileStream("c:\\intMap.txt", FileMode.Create); //создаем файловый поток
            StreamWriter writer = new StreamWriter(file); //создаем «потоковый писатель» и связываем его с файловым потоком  
            int cnt = 0;
            foreach (int element in RequiredParams)
            {
                if (cnt >= map.Count)
                    break;
                StringBuilder sb = new StringBuilder();
                sb.Append(element + "\t");
                sb.Append(map.ElementAt(cnt++));
                writer.WriteLine(sb.ToString()); //записываем в файл

            }
            writer.Close(); //закрываем поток. Не закрыв поток, в файл ничего не запишется 
        }

        public void writeDoubleValue(Dictionary<double, string> doubleValueMap)
        {
            FileStream file = new FileStream("c:\\doubleMap.txt", FileMode.Create); //создаем файловый поток
            StreamWriter writer = new StreamWriter(file); //создаем «потоковый писатель» и связываем его с файловым потоком  
            int cnt = 0;
            foreach (int element in RequiredParams)
            {
                if (cnt >= doubleValueMap.Count)
                    break;
                StringBuilder sb = new StringBuilder();
                sb.Append(element + "\t");
                sb.Append(doubleValueMap.ElementAt(cnt++));
                writer.WriteLine(sb.ToString()); //записываем в файл

            }
            writer.Close(); //закрываем поток. Не закрыв поток, в файл ничего не запишется 
        }
#endif


        public string determinateNameOfTool() {

            string nameTool = null;
            Tag tempCutterTag;
            Tool tempTool;

            if (Operation == null) 
                          return null;
            //создаем инструмент, затем пропускаем его через метод enc()
            ufs.Oper.AskCutterGroup(Operation.Tag, out tempCutterTag);
            tempTool = NXObjectManager.Get(tempCutterTag) as Tool;
            nameTool = enc(tempTool.Name);
            
            return nameTool;
        }

        public double determinateDiameterOfTool(string toolName)
        {
                Part refPart = NxSession.Part;
                double diam;
            
                // поиск инструмента в магазине и создание параметров для создания операции
                NXOpen.CAM.Tool tempTool = (NXOpen.CAM.Tool)refPart.CAMSetup.CAMGroupCollection.FindObject(toolName);
                
                NXOpen.CAM.MillingToolBuilder millingToolBuilder;
                millingToolBuilder = NxSession.Part.CAMSetup.CAMGroupCollection.CreateMillToolBuilder(tempTool);
                diam = millingToolBuilder.TlDiameterBuilder.Value;
                    
                millingToolBuilder.Destroy();

                return diam;            
        }


        public void writeBooleanValue(Dictionary<int, string> map)
        {
            FileStream file = new FileStream("c:\\booleanMap.txt", FileMode.Create);
            StreamWriter writer = new StreamWriter(file);
            
                foreach (int key in map.Keys)
                {
                        writer.WriteLine(map[key]);
                    }
                
                writer.Close();
            }
      


        private int GetWCSNumber() { 
        
        Tag tagMCS;
        ufs.Param.AskParamDefiner(Operation.Tag, UFConstants.UF_PARAM_MCS, out tagMCS);
        NXOpen.CAM.CAMObject[] CAMObj = new NXOpen.CAM.CAMObject[1];
        CAMObj[0] = Operation;
       
            
        NXOpen.CAM.TurnOrientGeomBuilder turnOrientGeomBuilder 
                  = NxSession.Part.CAMSetup.CAMGroupCollection.CreateTurnOrientGeomBuilder(CAMObj[0]);

       int x = turnOrientGeomBuilder.FixtureOffsetBuilder.Value;
       turnOrientGeomBuilder.Destroy();

        return x;
        }

        //метод определяет повернут ли инстр вокруг своей оси и угол ориентации державки
      private double GetCutterAngleOrientAndFlipToolAroundHolder(){

          double value = 0;
          NXOpen.CAM.TurningOperationBuilder turningBuilder;

          if (ufs == null || Operation == null)
              return value;

          switch (UF_TYPE_OPERATION_NAME)
          {
              case "RoughTurning":
          
                  turningBuilder = NxSession.Part.CAMSetup.CAMOperationCollection.CreateRoughTurningBuilder(Operation);
                  UF_PARAM_TL_FlipToolAroundHolder = turningBuilder.FlipToolAroundHolder; // определяем разворот инструмента
                  if (turningBuilder.ReorientToolHolder == true)
                        value = Math.Round(turningBuilder.ToolHolderAngle.Value, 3);
                  
                  turningBuilder.Destroy();
                  return value;

              case "FinishTurning":
                  turningBuilder = NxSession.Part.CAMSetup.CAMOperationCollection.CreateFinishTurningBuilder(Operation);
                  UF_PARAM_TL_FlipToolAroundHolder = turningBuilder.FlipToolAroundHolder; // определяем разворот инструмента
                  if (turningBuilder.ReorientToolHolder == true)
                        value = Math.Round(turningBuilder.ToolHolderAngle.Value, 3);
                  turningBuilder.Destroy();
                  return value;
                                    
              case "ThreadTurning":
                  turningBuilder = NxSession.Part.CAMSetup.CAMOperationCollection.CreateThreadTurningBuilder(Operation);
                  UF_PARAM_TL_FlipToolAroundHolder = turningBuilder.FlipToolAroundHolder; // определяем разворот инструмента
                  if (turningBuilder.ReorientToolHolder == true)
                         value = Math.Round(turningBuilder.ToolHolderAngle.Value, 3);
                  turningBuilder.Destroy();
                  return value;

              case "Operation":
                  NXOpen.CAM.TeachmodeTurningBuilder teachmodeTurningBuilder;
                  teachmodeTurningBuilder = NxSession.Part.CAMSetup.CAMOperationCollection.CreateTeachmodeTurningBuilder(Operation);
                  UF_PARAM_TL_FlipToolAroundHolder = teachmodeTurningBuilder.FlipToolAroundHolder; // определяем разворот инструмента
                  value = Math.Round(teachmodeTurningBuilder.ToolHolderAngle.Value, 3);
                  teachmodeTurningBuilder.Destroy();
                  return value;

              default:
                  return value;
          }
      }


      private double GetTL_MaxReach() {

          double value = -1;
          NXOpen.CAM.ThreadToolBuilder threadToolBuilder = null;
          NXOpen.CAM.TurnToolBuilder turnToolBuilder = null;
          if (Tool == null)
              return value;
          
              if (CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_THREAD)
                 {
                  threadToolBuilder = NxSession.Part.CAMSetup.CAMGroupCollection.CreateThreadToolBuilder(Tool);
                  if (threadToolBuilder.MaxDepthToggle)
                      return Math.Round(threadToolBuilder.MaxDepthBuilder.Value);
                  }

              else { 
                  turnToolBuilder = NxSession.Part.CAMSetup.CAMGroupCollection.CreateTurnToolBuilder(Tool);
                  
                  if (turnToolBuilder.MaxToolReachToggle)                                             // если галочка зоны досягаемости активна 
                  return Math.Round(turnToolBuilder.MaxToolReachBuilder.Value);                       // получаем значение зоны досягаемости
                   }
              return value;
      }

        private bool GetBoolParams(int paramIndex)
        {
            if (ufs == null || RequiredParams == null || Operation == null || !RequiredParams.Contains(paramIndex)) 
                return false;
            
            bool value;
                   
            ufs.Param.AskLogicalValue(Operation.Tag, paramIndex, out value);
            return value;
        }


        private void GetRequiredParams()
        {
            if (ufs == null) return;
            int cnt;
            int[] indices;
           
            ufs.Param.AskRequiredParams(Operation.Tag, out cnt, out indices);
            RequiredParams = new List<int>(indices);  
        }


        public static string enc(string str)
        {
            return Encoding.UTF8.GetString(Encoding.GetEncoding(1251).GetBytes(str));

        }

        public string newEncoder(string s) {

            byte[] data = Encoding.Default.GetBytes(s);


            if (data.Length > 2 && data[0] == 0xef && data[1] == 0xbb && data[2] == 0xbf)
            {
                if (data.Length != 3) return Encoding.UTF8.GetString(data, 3, data.Length - 3);
                else return "";
            }

            int i = 0;
            while (i < data.Length - 1)
            {
                if (data[i] > 0x7f)
                { // не ANSI-символ
                    if ((data[i] >> 5) == 6)
                    {
                        if ((i > data.Length - 2) || ((data[i + 1] >> 6) != 2))
                            return Encoding.GetEncoding(1251).GetString(data);
                        i++;
                    }
                    else if ((data[i] >> 4) == 14)
                    {
                        if ((i > data.Length - 3) || ((data[i + 1] >> 6) != 2) || ((data[i + 2] >> 6) != 2))
                            return Encoding.GetEncoding(1251).GetString(data);
                        i += 2;
                    }
                    else if ((data[i] >> 3) == 30)
                    {
                        if ((i > data.Length - 4) || ((data[i + 1] >> 6) != 2) || ((data[i + 2] >> 6) != 2) || ((data[i + 3] >> 6) != 2))
                            return Encoding.GetEncoding(1251).GetString(data);
                        i += 3;
                    }
                    else
                    {
                        return Encoding.GetEncoding(1251).GetString(data);
                    }
                }
                i++;
            }

            return Encoding.UTF8.GetString(data);

        }

        public static string EncodeStr(string value)
        {
            return Encoding.UTF8.GetString(Encoding.Default.GetBytes(value));
        }

        private void GetCutterInsertTypeStr()
        {
            if (ufs == null) return;
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
                        case UFConstants.UF_TURN_INSERTTYPE_RECTANGLE: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - прямоугольник ISO L"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_86: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - ромб 86<$s>  ISO M"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_OCTAGON: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - восьмиугольник ISO O"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_PENTAGON: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - пятиугольник ISO P"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_ROUND: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - круглая ISO R"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_SQUARE: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - квадрат ISO S"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_TRIANGLE: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - треугольная ISO T"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_DIAMOND_35: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - ромб 35<$s>  ISO V"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_TRIGON: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - ломаный треугольник ISO W"; return;
                        case UFConstants.UF_TURN_INSERTTYPE_USER: UF_PARAM_TL_INSERTTYPE_STR = "Вставка - произвольный профиль"; return;
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

        private string GetToolNumber()
        {
            if (_operationGroup == null) 
                return null;

            var machineName = _operationGroup.MachineName;
            if (machineName.Contains("QTN") || machineName.Contains("QTS"))
            {
                //var holderNumber = Math.Abs(UF_PARAM_TL_HOLDER_NUMBER) > 0 ? UF_PARAM_TL_HOLDER_NUMBER : 1;
                var holderNumber = UF_PARAM_TL_HOLDER_NUMBER;
                var s = holderNumber > 0 && holderNumber < 192
                    ? string.Format("T{0:D2}.{1:D2} ({2})", UF_PARAM_TL_NUMBER, holderNumber, Convert.ToChar(holderNumber + 64))
                    : (holderNumber == 0
                        ? string.Format("T{0:D2}.{1:D2}", UF_PARAM_TL_NUMBER, holderNumber)
                        : "Номер держателя задан не верно!");
                return s;
            }
            if (machineName.Contains("CTX beta 1250 TC 4A"))
            {
                return ChanellNumber == 0
                    ? string.Empty
                    : (ChanellNumber == 1
                    ? (Tool != null ? Tool.Name : string.Empty)
                        : UF_PARAM_TL_NUMBER.ToString());
            }
            if ((machineName.Contains("CTX beta 1250 TC") || machineName.Contains("DMU 50 eco")
                ||machineName.Contains("CTX 800 TC")) 
                && !machineName.Contains("CTX beta 1250 TC 4A"))
            {
                if (Tool != null) return Tool.Name;
            }

            return "T " + UF_PARAM_TL_NUMBER;
        }

        public int ChanellNumber { get; set; }
    }
}