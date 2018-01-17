using System;
using System.Collections.Generic;
using System.Linq;
using NXOpen;
using NXOpen.UF;
using TechDocNS.Model;
using System.Diagnostics;
using System.Text;


namespace TechDocNS.Services
{
    public class NxOperationDescriptionsService
    {
        private readonly List<string> _toolsCardAttributesFilter;
        //создаём мапу где будем хранить праметры инструмента для вывода в КИ
        Dictionary<Tag, List<string>> toolMap;
        Dictionary<Tag, bool> cutComParams; //мапа для хранения параметров коррекции
        int[] arrForCheckToolCorrection = new int[]{UFConstants.UF_CUTTER_TYPE_MILL,
                                                    UFConstants.UF_CUTTER_TYPE_BARREL,
                                                    UFConstants.UF_CUTTER_TYPE_T,  15,
                                                    UFConstants.UF_CUTTER_TYPE_DRILL};

        public NxOperationDescriptionsService()
        {
            _toolsCardAttributesFilter = NxSession.GetToolsCardAttributesFilter();
        }

        public IEnumerable<string[]> GetDescriptions(IEnumerable<NxOperationGroup> groups)
        {
            var nxOperationGroups = groups as IList<NxOperationGroup> ?? groups.ToList();
            if (groups == null || !nxOperationGroups.Any()) return null;

            var descriptions = nxOperationGroups
                .SelectMany(GetOperationDescriptions)
                .Select((s, i) =>
                {
                    // пронумеровать все строки с описаниями
                    s[0] += string.Format(" {0:D2}", i + 1);
                    return s;
                });

            return descriptions;
        }

        private IEnumerable<string[]> GetOperationDescriptions(NxOperationGroup operationGroup)
        {
            string chanellName = null;

            // заголовок для всех операций
            var list = new List<string[]>(7)
            {
                new[] {"У", "-", operationGroup.Name, operationGroup.Description, string.Empty, string.Empty, string.Empty}
            };


            // сортируем по номеру канала
            var nxOperations = operationGroup.NxOperations.OrderBy(op => op.ChanellNumber);

            // группируем операции по инструменту
            var operationsGroup = nxOperations.GroupBy(op => op.CUTTER_TAG);
            foreach (var operations in operationsGroup)
            {
                foreach(var oper in operations){
                    
                    //если коррекция включена, делаем проверку на правильную трассировку одного и того же инстр, входящего в массив
                    if(arrForCheckToolCorrection.Contains( oper.CUTTER_TYPE) && oper.getCutComParams())
                        try
                          {cutComParambuilder(oper);} 
                        catch(NXException e)
                          {throw;}
                }

                var operation = operations.FirstOrDefault();
                if (operation == null) continue;

                // довольно сложно будем добавлять признак первого или второго канала для станка
                var s = operation.ChanellNumber == 1 ? "Первый канал" : (operation.ChanellNumber == 2 ? "Второй канал" : null);
                if (chanellName != s)
                {
                    chanellName = s;
                    list.Add(new[] { string.Empty, "-", string.Empty, chanellName, string.Empty, string.Empty, string.Empty });
                }

                if ( operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_TURN ||
                     operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_GROOVE ||
                     operation.CUTTER_TYPE== UFConstants.UF_CUTTER_TYPE_THREAD)
                {
                    toolMap = operation.getToolListRegister(); // инициализируем мапу если выполняются условия
                }

                    
                // получим описание операции в зависипости от типа инструмента
                switch (operation.CUTTER_TYPE)
                {
                    case UFConstants.UF_CUTTER_TYPE_MILL:
                    case UFConstants.UF_CUTTER_TYPE_BARREL:
                    case UFConstants.UF_CUTTER_TYPE_T:
                    //----    
                    case 15:        //добавил условие для Switch тип режущего инструмента == 15
                    //----
                        list.AddRange(GetDescription_UF_CUTTER_TYPE_MILL(operations)); break;

                    case UFConstants.UF_CUTTER_TYPE_DRILL:
                        list.AddRange(GetDescription_UF_CUTTER_TYPE_DRILLL(operations)); break;

                    case UFConstants.UF_CUTTER_TYPE_TURN:
                        list.AddRange(GetDescription_UF_CUTTER_TYPE_TURN(operations)); break;

                    case UFConstants.UF_CUTTER_TYPE_GROOVE:
                        list.AddRange(GetDescription_UF_CUTTER_TYPE_GROOVE(operations)); break;

                    case UFConstants.UF_CUTTER_TYPE_THREAD:
                        list.AddRange(GetDescription_UF_CUTTER_TYPE_THREAD(operations)); break;

                    case UFConstants.UF_CUTTER_TYPE_FORM:
                        list.AddRange(GetDescription_UF_CUTTER_TYPE_FORM(operations)); break;

                    case UFConstants.UF_CUTTER_TYPE_SOLID:
                        list.AddRange(GetDescription_UF_CUTTER_TYPE_SOLID(operations)); break;
                }
                
                // для всех инструментов
                list.AddRange(GetAdditionalOperationDescriptions(operation));

                // получим атрибуты инструмента
                list.AddRange(GetToolAttributes(operation));
            }
            return list;
        }


        private void cutComParambuilder(NxOperation oper){
            if (cutComParams == null)
                cutComParams = new Dictionary<Tag, bool>();

            switch (cutComParams.Keys.Contains(oper.CUTTER_TAG))
            {
                case true:
                    cutComEquals(oper);
                    break;
                default: // заносим в мапу значение трассировки при коррекции
                    cutComParams.Add(oper.CUTTER_TAG, oper.UF_PARAM_TL_CutcomOutputContactPoint);
                    break;
            }
        }
        // метод сравнивающий парметры трассировки при включенной, должен выполняться при включённой коррекции
        private void cutComEquals(NxOperation oper) {
            
            if (cutComParams[oper.CUTTER_TAG] != oper.UF_PARAM_TL_CutcomOutputContactPoint)
                throw new Exception(string.Format(
                    "--> Инструмент \"{0}\" <-- при включенной коррекции \n" +
                    "выбраны разные режимы трассировки, что может привести к зарезу " +
                    "------------------------------------------------------------------\n" +
                    "справка: для корректной работы приложения проверьте \n" +
                    "соответствие вывода данных контакта/трассировки",oper.Tool.Name));
        }

        private decimal RadiansToDegrees(double radians)
        {
            var d = radians * (180 / Math.PI);
            return Math.Round((decimal)d, 1);
        }

        private IEnumerable<string[]> GetAdditionalOperationDescriptions(NxOperation operation)
        {
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(operation.UF_PARAM_TL_TEXT))
                list.Add(operation.UF_PARAM_TL_TEXT);

            if (!string.IsNullOrWhiteSpace(operation.UF_PARAM_TL_HOLDER_DESCRIPTION))
                list.Add("Держатель " + operation.UF_PARAM_TL_HOLDER_DESCRIPTION);

            return
                list.Select(
                    s =>
                        new[]
                        {
                            string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty
                        });
        }

        private IEnumerable<string[]> GetToolAttributes(NxOperation operation)
        {
            var attributes = operation.CUTTER_ATTRIBUTES
                .Where(attr => !string.IsNullOrWhiteSpace(attr.Title) && _toolsCardAttributesFilter != null && !_toolsCardAttributesFilter.Contains(attr.Title))
                .Select(attr =>
                    attr.Title.Contains("ID_TOOL")
                        ? "Код MAX инструмента = " + attr.StringValue
                        : (attr.Title.Contains("ID_INSERT")
                            ? "Код MAX реж. вставки = " + attr.StringValue
                            : attr.StringValue)
                );

            return
                attributes.Select(
                    s =>
                        new[]
                        {
                            string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty
                        });
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_SOLID(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            if (operation == null) return list;

            var s = "Щуп электронный";
            list.Add(new[] { "Т", "-", operation.ToolNumber, s, string.Empty, string.Empty, string.Empty });

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_FORM(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            var str = string.Empty;
            if (operation == null) return list;

            var s = "Резец фасонный";
            str = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("Зона досягаемости {0} мм", operation.UF_PARAM_TL_MaxReach) : string.Empty;

            list.Add(new[] { "Т", "-", operation.ToolNumber, s, str, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_THREAD(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            var str = string.Empty;
                        
            if (operation == null) return list;

            
                List<string> descriptionMap = toolMap[operation.CUTTER_TAG]; // достаём  из мапы по ключу значение и записываем его в лист 
            
            
            var s = string.Format("Угол профиля={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_LEFT_ANG));
            
            list.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вставки - верхняя сторона" : "Позиция вставки - нижняя сторона";
            str = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("Максимальная глубина {0}мм", operation.UF_PARAM_TL_MaxReach) : string.Empty;
            
            list.Add(new[] { string.Empty, "-", string.Empty, s, str, string.Empty, string.Empty });

            //------------------------------
            s = operation.UF_PARAM_TL_FlipToolAroundHolder == false ? "Стандартный" : "Обратный";
            
            list.Add(new[] { string.Empty, "-", string.Empty, s , string.Empty, string.Empty, string.Empty });
            //------------------------------

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_GROOVE(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            var str = string.Empty;
            var str1 = string.Empty;
            var s2 = string.Empty;
            List<string> descriptionMap = null;
            
            if (operation == null) return list;
            
               descriptionMap = toolMap[operation.CUTTER_TAG]; // достаём  из мапы по ключу значение и записываем его в лист 
                        
            var s = string.Format("Ширина={0:F2} мм.", operation.UF_PARAM_TL_INSERT_WIDTH);
            list.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });
            
            if(operation.UF_PARAM_TL_RADIUS == -1)
                s2 = string.Format("Rверш.={0:F2} мм.", operation.UF_PARAM_TL_INSERT_WIDTH/2);
            else  
                s2 = string.Format("Rверш.={0:F2} мм.", operation.UF_PARAM_TL_RADIUS); //UF_PARAM_TL_LEFT_COR_RAD //UF_PARAM_TL_NOSE_RAD

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
            list.Add(new[] { string.Empty, "-", string.Empty, s, s2, string.Empty, string.Empty });

            //------------------------------
            s = operation.UF_PARAM_TL_FlipToolAroundHolder == false ? "Стандартный" : "Обратный";

            str1 = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("Зона досягаемости {0} мм", operation.UF_PARAM_TL_MaxReach) : string.Empty;

            list.Add(new[] { string.Empty, "-", string.Empty, s , str1, string.Empty, string.Empty });
            //------------------------------

            foreach (string decsrReg in descriptionMap)
            {
                list.Add(new[] { string.Empty, "-", string.Empty, decsrReg, string.Empty, string.Empty, string.Empty });
            }
            //------------------------------
            
            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_TURN(IGrouping<Tag, NxOperation> operations)
        {   
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            if (operation == null) return list;
            List<string> descriptionMap = null;
            
            descriptionMap = toolMap[operation.CUTTER_TAG]; // достаём  из мапы по ключу значение и записываем его в лист 
            
            var s = "";
            var str = string.Empty;

            switch (operation.CUTTER_SUBTYPE)
            {
                case UFConstants.UF_CUTTER_SUBTYPE_TURN_STD:
                case UFConstants.UF_CUTTER_SUBTYPE_TURN_BORING_BAR:
                    s = string.Format("Rверш.={0:F2} мм.", operation.UF_PARAM_TL_NOSE_RAD);
                    break;
                case UFConstants.UF_CUTTER_SUBTYPE_TURN_BUTTON:
                    s = string.Format("<o> верш.={0:F2}", operation.UF_PARAM_TL_BUTTON_DIAMETER);
                    break;
            }

            list.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, "" });

            s = string.IsNullOrEmpty(operation.UF_PARAM_TL_INSERTTYPE_STR) ? string.Empty : operation.UF_PARAM_TL_INSERTTYPE_STR;


            str = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("Зона досягаемости {0} мм", operation.UF_PARAM_TL_MaxReach): string.Empty;
            
            list.Add(new[] { string.Empty, "-", string.Empty, s, str, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            //------------------------------            
            foreach (string decsrReg in descriptionMap)
            {
                list.Add(new[] { string.Empty, "-", string.Empty, decsrReg, string.Empty, string.Empty, string.Empty }); 
            }
            //------------------------------
            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_DRILLL(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var str = string.Empty; ;
            var operation = operations.FirstOrDefault();
            if (operation == null) return list;

            var s = string.Format("<o>={0:F2}", operation.UF_PARAM_TL_DIAMETER);
            if (Math.Abs(operation.UF_PARAM_TL_ZMOUNT) > 0)
                s += string.Format("; вылет={0:F2}", operation.UF_PARAM_TL_ZMOUNT);
            var s2 = "Н/А";
            list.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, s2 });

            s = string.Empty;
            if (Math.Abs(operation.UF_PARAM_TL_FLUTE_LN) > 0)
                s += string.Format("Длина реж. кромки={0:F2}; ", operation.UF_PARAM_TL_FLUTE_LN);
            s += string.Format("Кол. зубьев={0}", operation.UF_PARAM_TL_NUM_FLUTES);

            switch (operation.CUTTER_SUBTYPE)
            {
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_STD:
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_CENTER_BELL:
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_SPOT_DRILL:
                    {
                        s += string.Format("; Угол заточки вершины={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_POINT_ANG));
                        break;
                    }

                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_COUNTERSINK:
                    {
                        s += string.Format("; Угол заточки вершины={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_POINT_ANG_COUNTERSINK));
                        break;
                    }

                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_TAP:
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_THREAD_MILL:
                    {
                        s += string.Format("; Шаг резьбы={0:F4} мм.", operation.UF_PARAM_TL_PITCH);
                        var s1 = operation.UF_PARAM_TL_DIRECTION == 1 ? "Правое" : "Левое";
                        s += string.Format("; Направление вращения={0}", s1);
                        break;
                    }
            }
            var strings = ParseDescriptionString(s);
            list.AddRange(strings.Select(s1 => new[] { "", "-", string.Empty, s1, string.Empty, string.Empty, string.Empty }));


            if (operation.getCutComParams())
            {
                str = "Работает с корректором, ";
                str += operation.UF_PARAM_TL_CutcomOutputContactPoint ? "ввести в таблицу диаметр/радиус инструмента"
                    : "ввести в таблицу диаметр/радиус = 0";

              list.Add(new[] { string.Empty, "-", string.Empty, str, string.Empty, string.Empty, string.Empty });
            }
            //-----------------------------------------------------
          
            return list;

        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_MILL(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var str = string.Empty;
            var operation = operations.FirstOrDefault();
            if (operation == null) return list;

            var s = string.Format("<o>={0:F2}", operation.UF_PARAM_TL_DIAMETER);

            if (operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_MILL && operation.CUTTER_SUBTYPE == UFConstants.UF_CUTTER_SUBTYPE_MILL_BALL)
                s = string.Format("<o>={0:F2}", operation.UF_PARAM_TL_COR1_RAD * 2);

            if (Math.Abs(operation.UF_PARAM_TL_ZMOUNT) > 0)
                s += string.Format("; вылет={0:F2}", operation.UF_PARAM_TL_ZMOUNT);
            var s2 = "Н/А";
            var find = operations.FirstOrDefault(op => op.UF_PARAM_CUTCOM_REGISTER_NUM >= 0);
            if (find != null) s2 = find.UF_PARAM_CUTCOM_REGISTER_NUM != 0 ? find.UF_PARAM_CUTCOM_REGISTER_NUM.ToString() : find.UF_PARAM_TL_NUMBER.ToString();

            list.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, s2 });

            s = string.Format("Длина реж. кромки={0:F2}; Кол. зубьев={1}", operation.UF_PARAM_TL_FLUTE_LN, operation.UF_PARAM_TL_NUM_FLUTES);

            switch (operation.CUTTER_TYPE)
            {
                case UFConstants.UF_CUTTER_TYPE_MILL:
                    switch (operation.CUTTER_SUBTYPE)
                    {
                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_5:
                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_7:
                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_10:
                            {
                                if (Math.Abs(operation.UF_PARAM_TL_COR1_RAD) > 0)
                                    s += string.Format("; Радиус скругления={0:F2} мм.", operation.UF_PARAM_TL_COR1_RAD);
                                if (Math.Abs(operation.UF_PARAM_TL_TIP_ANG) > 0)
                                    s += string.Format("; Угол при вершине={0:F2}<$s>", (90 - RadiansToDegrees(operation.UF_PARAM_TL_TIP_ANG) * 2));
                                if (Math.Abs(operation.UF_PARAM_TL_TAPER_ANG) > 0)
                                    s += string.Format("; Угол конуса={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_TAPER_ANG) * 2);
                            }
                            break;

                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_BALL:
                            {
                                //s += string.Format("; Радиус сферы={0:F2} мм.", operation.UF_PARAM_TL_DIAMETER / 2);
                                s += string.Format("; Радиус сферы={0:F2} мм.", operation.UF_PARAM_TL_COR1_RAD);
                                if (Math.Abs(operation.UF_PARAM_TL_TAPER_ANG) > 0)
                                    s += string.Format("; Угол конуса={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_TAPER_ANG) * 2);
                            }
                            break;

                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_CHAMFER:
                            if (Math.Abs(operation.UF_PARAM_TL_TAPER_ANG) > 0)
                                s += string.Format("; Угол фаски={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_TAPER_ANG) * 2);
                            break;

                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_SPHERICAL:
                            {
                                s += string.Format("; Радиус сферы={0:F2} мм.", operation.UF_PARAM_TL_DIAMETER / 2);
                                if (Math.Abs(operation.UF_PARAM_TL_SHANK_DIA) > 0)
                                    s += string.Format("; Диаметр шейки={0:F2} мм.", RadiansToDegrees(operation.UF_PARAM_TL_SHANK_DIA) * 2);
                            }
                            break;
                    }
                    break;
                case UFConstants.UF_CUTTER_TYPE_BARREL:
                    {
                        if (Math.Abs(operation.UF_PARAM_TL_BARREL_RAD) > 0)
                            s += string.Format("; Радиус бочки={0:F2} мм.", operation.UF_PARAM_TL_BARREL_RAD);
                    }
                    break;
                case UFConstants.UF_CUTTER_TYPE_T:
                    {
                        if (Math.Abs(operation.UF_PARAM_TL_LOW_COR_RAD) > 0)
                            s += string.Format("; Нижний радиус скругления={0:F2} мм.", operation.UF_PARAM_TL_LOW_COR_RAD);
                        if (Math.Abs(operation.UF_PARAM_TL_UP_COR_RAD) > 0)
                            s += string.Format("; Верхний радиус скругления={0:F2} мм.", operation.UF_PARAM_TL_UP_COR_RAD);
                    }
                    break;
            }
            var strings = ParseDescriptionString(s);
            list.AddRange(strings.Select(s1 => new[] { string.Empty, "-", string.Empty, s1, string.Empty, string.Empty, string.Empty }));

            //-----------------------------------------------------

           if (operation.getCutComParams()) 
            {
                str = "Работает с корректором, ";
                str += operation.UF_PARAM_TL_CutcomOutputContactPoint ? "ввести в таблицу диаметр/радиус инструмента"
                    : "ввести в таблицу диаметр/радиус = 0";
            list.Add(new[] { string.Empty, "-", string.Empty, str, string.Empty, string.Empty, string.Empty });            
           }
            
            return list;
        }


        private static IEnumerable<string> ParseDescriptionString(string s)
        {
            if (s.Length < 60) return new[] { s };

            var ret = new List<string>();
            var s1 = s;
            while (s1.Length >= 60)
            {
                var indexOflastSpace = s1.LastIndexOf(";", StringComparison.Ordinal);
                ret.Add(s1.Substring(0, indexOflastSpace + 1));
                if (s1.Length > indexOflastSpace + 1) 
                    s1 = s1.Substring(indexOflastSpace + 1).Trim();
            }
            ret.Add(s1);
            return ret.ToArray();
        }

    }
}