using System;
using System.Collections.Generic;
using System.Linq;
using NXOpen;
using NXOpen.UF;
using TechDocNS.Model;

namespace TechDocNS.Services
{
    public class OperationDescriptionsService
    {
        private readonly List<string> _toolsCardAttributesFilter;

        public OperationDescriptionsService()
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
                var operation = operations.FirstOrDefault();
                if (operation == null) continue;

                // довольно сложно будем добавлять признак первого или второго канала для станка
                var s = operation.ChanellNumber == 1 ? "Первый канал" : (operation.ChanellNumber == 2 ? "Второй канал" : null);
                if (chanellName != s)
                {
                    chanellName = s;
                    list.Add(new[] { string.Empty, "-", string.Empty, chanellName, string.Empty, string.Empty, string.Empty });
                }

                // получим описание операции в зависипости от типа инструмента
                switch (operation.CUTTER_TYPE)
                {
                    case UFConstants.UF_CUTTER_TYPE_MILL:
                    case UFConstants.UF_CUTTER_TYPE_BARREL:
                    case UFConstants.UF_CUTTER_TYPE_T:
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
            if (operation == null) return list;

            var s = "Резец фасонный";
            list.Add(new[] { "Т", "-", operation.ToolNumber, s, string.Empty, string.Empty, string.Empty });

            s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_THREAD(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            if (operation == null) return list;

            var s = string.Format("Угол профиля={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_LEFT_ANG));
            list.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });

            s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вставки - верхняя сторона" : "Позиция вставки - нижняя сторона";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_GROOVE(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            if (operation == null) return list;

            var s = string.Format("Ширина={0:F2} мм.", operation.UF_PARAM_TL_INSERT_WIDTH);
            list.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });
            var s2 = string.Format("Rверш.={0:F2} мм.", operation.UF_PARAM_TL_RADIUS); //UF_PARAM_TL_LEFT_COR_RAD //UF_PARAM_TL_NOSE_RAD
            //OperationDescriptions.Add(new[] { "", "-", string.Empty, string.Empty, s, string.Empty, string.Empty });

            s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
            list.Add(new[] { "", "-", string.Empty, s, s2, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_TURN(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            if (operation == null) return list;

            var s = "";
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
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_DRILLL(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();

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
            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_MILL(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();

            var operation = operations.FirstOrDefault();
            if (operation == null) return list;

            var s = string.Format("<o>={0:F2}", operation.UF_PARAM_TL_DIAMETER);
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
                                s += string.Format("; Радиус сферы={0:F2} мм.", operation.UF_PARAM_TL_DIAMETER / 2);
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


        /*        private void GetAdditionalOperationDescriptions(NxOperation operation)
              {
                  if (!string.IsNullOrWhiteSpace(operation.UF_PARAM_TL_TEXT))
                      OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, operation.UF_PARAM_TL_TEXT, string.Empty, string.Empty, string.Empty });

                  if (!string.IsNullOrWhiteSpace(operation.UF_PARAM_TL_HOLDER_DESCRIPTION))
                      OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, "Держатель " + operation.UF_PARAM_TL_HOLDER_DESCRIPTION, string.Empty, string.Empty, string.Empty });
              }
      */
    

        /*
                private void EnumerateDescriptions()
                {
                    for (var i = 0; i < OperationDescriptions.Count; i++)
                        OperationDescriptions[i][0] += string.Format(" {0:D2}", i + 1);
                }
        */

        /*
                private void EnumerateDescriptions(ref int start)
                {
                    for (var i = 0; i < OperationDescriptions.Count; i++)
                        OperationDescriptions[i][0] += string.Format(" {0:D2}", start + i + 1);
                    start = OperationDescriptions.Count;
                }
        */

        /*        private void GetToolAttributes(NxOperation operation)
                {
                    foreach (var attr in operation.CUTTER_ATTRIBUTES)
                    {
                        if (ToolsCardAttributesFilter != null && ToolsCardAttributesFilter.Contains(attr.Title)) continue;
                        var s = attr.StringValue; //attr.Title + ": " + attr.StringValue;
                        if (attr.Title.Contains("ID_TOOL")) s = "Код MAX инструмента = " + attr.StringValue;
                        if (attr.Title.Contains("ID_INSERT")) s = "Код MAX реж. вставки = " + attr.StringValue;
                        OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
                    }
                }
        */
       

        /*
                private void GetDescription_UF_CUTTER_TYPE_SOLID(Tag toolTag)
                {
                    var operation = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag);
                    var s = "Щуп электронный";
                    OperationDescriptions.Add(new[] { "Т", "-", operation.ToolNumber, s, string.Empty, string.Empty, string.Empty });
                }
        */

        
        /*
                private void GetDescription_UF_CUTTER_TYPE_FORM(Tag toolTag)
                {
                    var operation = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag);
                    var s = "Резец фасонный";
                    OperationDescriptions.Add(new[] { "Т", "-", operation.ToolNumber, s, string.Empty, string.Empty, string.Empty });

                    s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

                    s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
                }
        */


        
        /*
                private void GetDescription_UF_CUTTER_TYPE_THREAD(Tag toolTag)
                {
                    var operation = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag);
                    var s = string.Format("Угол профиля={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_LEFT_ANG));
                    OperationDescriptions.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });

                    s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

                    s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вставки - верхняя сторона" : "Позиция вставки - нижняя сторона";
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
                }
        */

        
        /*
                private void GetDescription_UF_CUTTER_TYPE_GROOVE(Tag toolTag)
                {
                    var operation = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag);
                    var s = string.Format("Ширина={0:F2} мм.", operation.UF_PARAM_TL_INSERT_WIDTH);
                    OperationDescriptions.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });
                    var s2 = string.Format("Rверш.={0:F2} мм.", operation.UF_PARAM_TL_RADIUS); //UF_PARAM_TL_LEFT_COR_RAD //UF_PARAM_TL_NOSE_RAD
                    //OperationDescriptions.Add(new[] { "", "-", string.Empty, string.Empty, s, string.Empty, string.Empty });

                    s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
                    OperationDescriptions.Add(new[] { "", "-", string.Empty, s, s2, string.Empty, string.Empty });

                    s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
                }
        */

        

        /*
                private void GetDescription_UF_CUTTER_TYPE_TURN(Tag toolTag)
                {
                    var operation = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag);
                    var s = "";
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

                    OperationDescriptions.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, "" });

                    s = string.IsNullOrEmpty(operation.UF_PARAM_TL_INSERTTYPE_STR) ? string.Empty : operation.UF_PARAM_TL_INSERTTYPE_STR;
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

                    s = "Угол установки держателя=" + RadiansToDegrees(operation.UF_PARAM_TL_TURN_HOLDER_ANGLE) + "<$s>";
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

                    s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "Позиция вcтавки - верхняя сторона" : "Позиция вcтавки - нижняя сторона";
                    OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });
                }
        */

        

        /*
                private void GetDescription_UF_CUTTER_TYPE_DRILLL(Tag toolTag)
                {
                    var operation = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag);
                    var s = string.Format("<o>={0:F2}", operation.UF_PARAM_TL_DIAMETER);
                    if (Math.Abs(operation.UF_PARAM_TL_ZMOUNT) > 0)
                        s += string.Format("; вылет={0:F2}", operation.UF_PARAM_TL_ZMOUNT);
                    var s2 = "Н/А";
                    OperationDescriptions.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, s2 });

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
                    foreach (var s1 in strings)
                        OperationDescriptions.Add(new[] { "", "-", string.Empty, s1, string.Empty, string.Empty, string.Empty });
                }
        */

       


        /*
                private void GetDescription_UF_CUTTER_TYPE_MILL(Tag toolTag)
                {
                    var operation = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag);

                    var s = string.Format("<o>={0:F2}", operation.UF_PARAM_TL_DIAMETER);
                    if (Math.Abs(operation.UF_PARAM_TL_ZMOUNT) > 0)
                        s += string.Format("; вылет={0:F2}", operation.UF_PARAM_TL_ZMOUNT);
                    var s2 = "Н/А";
                    var find = nxOperationGroups.NxOperations.Find(op => op.CUTTER_TAG == toolTag && op.UF_PARAM_CUTCOM_REGISTER_NUM >= 0);
                    if (find != null) s2 = find.UF_PARAM_CUTCOM_REGISTER_NUM != 0 ? find.UF_PARAM_CUTCOM_REGISTER_NUM.ToString() : find.UF_PARAM_TL_NUMBER.ToString();

                    OperationDescriptions.Add(new[] { "Т", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, s2 });

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
                                    s += string.Format("; Радиус сферы={0:F2} мм.", operation.UF_PARAM_TL_DIAMETER / 2);
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
                    foreach (var s1 in strings)
                        OperationDescriptions.Add(new[] { string.Empty, "-", string.Empty, s1, string.Empty, string.Empty, string.Empty });
                }
        */
    }
}