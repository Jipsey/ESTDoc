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
        //������ ���� ��� ����� ������� �������� ����������� ��� ������ � ��
        Dictionary<Tag, List<string>> toolMap;
        Dictionary<Tag, bool> cutComParams; //���� ��� �������� ���������� ���������
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
                    // ������������� ��� ������ � ����������
                    s[0] += string.Format(" {0:D2}", i + 1);
                    return s;
                });

            return descriptions;
        }

        private IEnumerable<string[]> GetOperationDescriptions(NxOperationGroup operationGroup)
        {
            string chanellName = null;

            // ��������� ��� ���� ��������
            var list = new List<string[]>(7)
            {
                new[] {"�", "-", operationGroup.Name, operationGroup.Description, string.Empty, string.Empty, string.Empty}
            };


            // ��������� �� ������ ������
            var nxOperations = operationGroup.NxOperations.OrderBy(op => op.ChanellNumber);

            // ���������� �������� �� �����������
            var operationsGroup = nxOperations.GroupBy(op => op.CUTTER_TAG);
            foreach (var operations in operationsGroup)
            {
                foreach(var oper in operations){
                    
                    //���� ��������� ��������, ������ �������� �� ���������� ����������� ������ � ���� �� �����, ��������� � ������
                    if(arrForCheckToolCorrection.Contains( oper.CUTTER_TYPE) && oper.getCutComParams())
                        try
                          {cutComParambuilder(oper);} 
                        catch(NXException e)
                          {throw;}
                }

                var operation = operations.FirstOrDefault();
                if (operation == null) continue;

                // �������� ������ ����� ��������� ������� ������� ��� ������� ������ ��� ������
                var s = operation.ChanellNumber == 1 ? "������ �����" : (operation.ChanellNumber == 2 ? "������ �����" : null);
                if (chanellName != s)
                {
                    chanellName = s;
                    list.Add(new[] { string.Empty, "-", string.Empty, chanellName, string.Empty, string.Empty, string.Empty });
                }

                if ( operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_TURN ||
                     operation.CUTTER_TYPE == UFConstants.UF_CUTTER_TYPE_GROOVE ||
                     operation.CUTTER_TYPE== UFConstants.UF_CUTTER_TYPE_THREAD)
                {
                    toolMap = operation.getToolListRegister(); // �������������� ���� ���� ����������� �������
                }

                    
                // ������� �������� �������� � ����������� �� ���� �����������
                switch (operation.CUTTER_TYPE)
                {
                    case UFConstants.UF_CUTTER_TYPE_MILL:
                    case UFConstants.UF_CUTTER_TYPE_BARREL:
                    case UFConstants.UF_CUTTER_TYPE_T:
                    //----    
                    case 15:        //������� ������� ��� Switch ��� �������� ����������� == 15
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
                
                // ��� ���� ������������
                list.AddRange(GetAdditionalOperationDescriptions(operation));

                // ������� �������� �����������
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
                default: // ������� � ���� �������� ����������� ��� ���������
                    cutComParams.Add(oper.CUTTER_TAG, oper.UF_PARAM_TL_CutcomOutputContactPoint);
                    break;
            }
        }
        // ����� ������������ �������� ����������� ��� ����������, ������ ����������� ��� ���������� ���������
        private void cutComEquals(NxOperation oper) {
            
            if (cutComParams[oper.CUTTER_TAG] != oper.UF_PARAM_TL_CutcomOutputContactPoint)
                throw new Exception(string.Format(
                    "--> ���������� \"{0}\" <-- ��� ���������� ��������� \n" +
                    "������� ������ ������ �����������, ��� ����� �������� � ������ " +
                    "------------------------------------------------------------------\n" +
                    "�������: ��� ���������� ������ ���������� ��������� \n" +
                    "������������ ������ ������ ��������/�����������",oper.Tool.Name));
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
                list.Add("��������� " + operation.UF_PARAM_TL_HOLDER_DESCRIPTION);

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
                        ? "��� MAX ����������� = " + attr.StringValue
                        : (attr.Title.Contains("ID_INSERT")
                            ? "��� MAX ���. ������� = " + attr.StringValue
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

            var s = "��� �����������";
            list.Add(new[] { "�", "-", operation.ToolNumber, s, string.Empty, string.Empty, string.Empty });

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_FORM(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            var str = string.Empty;
            if (operation == null) return list;

            var s = "����� ��������";
            str = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("���� ������������ {0} ��", operation.UF_PARAM_TL_MaxReach) : string.Empty;

            list.Add(new[] { "�", "-", operation.ToolNumber, s, str, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "������� �c����� - ������� �������" : "������� �c����� - ������ �������";
            list.Add(new[] { string.Empty, "-", string.Empty, s, string.Empty, string.Empty, string.Empty });

            return list;
        }

        private IEnumerable<string[]> GetDescription_UF_CUTTER_TYPE_THREAD(IGrouping<Tag, NxOperation> operations)
        {
            var list = new List<string[]>();
            var operation = operations.FirstOrDefault();
            var str = string.Empty;
                        
            if (operation == null) return list;

            
                List<string> descriptionMap = toolMap[operation.CUTTER_TAG]; // ������  �� ���� �� ����� �������� � ���������� ��� � ���� 
            
            
            var s = string.Format("���� �������={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_LEFT_ANG));
            
            list.Add(new[] { "�", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "������� ������� - ������� �������" : "������� ������� - ������ �������";
            str = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("������������ ������� {0}��", operation.UF_PARAM_TL_MaxReach) : string.Empty;
            
            list.Add(new[] { string.Empty, "-", string.Empty, s, str, string.Empty, string.Empty });

            //------------------------------
            s = operation.UF_PARAM_TL_FlipToolAroundHolder == false ? "�����������" : "��������";
            
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
            
               descriptionMap = toolMap[operation.CUTTER_TAG]; // ������  �� ���� �� ����� �������� � ���������� ��� � ���� 
                        
            var s = string.Format("������={0:F2} ��.", operation.UF_PARAM_TL_INSERT_WIDTH);
            list.Add(new[] { "�", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, string.Empty });
            
            if(operation.UF_PARAM_TL_RADIUS == -1)
                s2 = string.Format("R����.={0:F2} ��.", operation.UF_PARAM_TL_INSERT_WIDTH/2);
            else  
                s2 = string.Format("R����.={0:F2} ��.", operation.UF_PARAM_TL_RADIUS); //UF_PARAM_TL_LEFT_COR_RAD //UF_PARAM_TL_NOSE_RAD

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "������� �c����� - ������� �������" : "������� �c����� - ������ �������";
            list.Add(new[] { string.Empty, "-", string.Empty, s, s2, string.Empty, string.Empty });

            //------------------------------
            s = operation.UF_PARAM_TL_FlipToolAroundHolder == false ? "�����������" : "��������";

            str1 = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("���� ������������ {0} ��", operation.UF_PARAM_TL_MaxReach) : string.Empty;

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
            
            descriptionMap = toolMap[operation.CUTTER_TAG]; // ������  �� ���� �� ����� �������� � ���������� ��� � ���� 
            
            var s = "";
            var str = string.Empty;

            switch (operation.CUTTER_SUBTYPE)
            {
                case UFConstants.UF_CUTTER_SUBTYPE_TURN_STD:
                case UFConstants.UF_CUTTER_SUBTYPE_TURN_BORING_BAR:
                    s = string.Format("R����.={0:F2} ��.", operation.UF_PARAM_TL_NOSE_RAD);
                    break;
                case UFConstants.UF_CUTTER_SUBTYPE_TURN_BUTTON:
                    s = string.Format("<o> ����.={0:F2}", operation.UF_PARAM_TL_BUTTON_DIAMETER);
                    break;
            }

            list.Add(new[] { "�", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, "" });

            s = string.IsNullOrEmpty(operation.UF_PARAM_TL_INSERTTYPE_STR) ? string.Empty : operation.UF_PARAM_TL_INSERTTYPE_STR;


            str = operation.UF_PARAM_TL_MaxReach != -1 ? string.Format("���� ������������ {0} ��", operation.UF_PARAM_TL_MaxReach): string.Empty;
            
            list.Add(new[] { string.Empty, "-", string.Empty, s, str, string.Empty, string.Empty });

            s = operation.UF_PARAM_TL_INSERT_POSITION == 1 ? "������� �c����� - ������� �������" : "������� �c����� - ������ �������";
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
                s += string.Format("; �����={0:F2}", operation.UF_PARAM_TL_ZMOUNT);
            var s2 = "�/�";
            list.Add(new[] { "�", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, s2 });

            s = string.Empty;
            if (Math.Abs(operation.UF_PARAM_TL_FLUTE_LN) > 0)
                s += string.Format("����� ���. ������={0:F2}; ", operation.UF_PARAM_TL_FLUTE_LN);
            s += string.Format("���. ������={0}", operation.UF_PARAM_TL_NUM_FLUTES);

            switch (operation.CUTTER_SUBTYPE)
            {
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_STD:
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_CENTER_BELL:
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_SPOT_DRILL:
                    {
                        s += string.Format("; ���� ������� �������={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_POINT_ANG));
                        break;
                    }

                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_COUNTERSINK:
                    {
                        s += string.Format("; ���� ������� �������={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_POINT_ANG_COUNTERSINK));
                        break;
                    }

                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_TAP:
                case UFConstants.UF_CUTTER_SUBTYPE_DRILL_THREAD_MILL:
                    {
                        s += string.Format("; ��� ������={0:F4} ��.", operation.UF_PARAM_TL_PITCH);
                        var s1 = operation.UF_PARAM_TL_DIRECTION == 1 ? "������" : "�����";
                        s += string.Format("; ����������� ��������={0}", s1);
                        break;
                    }
            }
            var strings = ParseDescriptionString(s);
            list.AddRange(strings.Select(s1 => new[] { "", "-", string.Empty, s1, string.Empty, string.Empty, string.Empty }));


            if (operation.getCutComParams())
            {
                str = "�������� � �����������, ";
                str += operation.UF_PARAM_TL_CutcomOutputContactPoint ? "������ � ������� �������/������ �����������"
                    : "������ � ������� �������/������ = 0";

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
                s += string.Format("; �����={0:F2}", operation.UF_PARAM_TL_ZMOUNT);
            var s2 = "�/�";
            var find = operations.FirstOrDefault(op => op.UF_PARAM_CUTCOM_REGISTER_NUM >= 0);
            if (find != null) s2 = find.UF_PARAM_CUTCOM_REGISTER_NUM != 0 ? find.UF_PARAM_CUTCOM_REGISTER_NUM.ToString() : find.UF_PARAM_TL_NUMBER.ToString();

            list.Add(new[] { "�", "-", operation.ToolNumber, operation.UF_PARAM_TL_DESCRIPTION, s, string.Empty, s2 });

            s = string.Format("����� ���. ������={0:F2}; ���. ������={1}", operation.UF_PARAM_TL_FLUTE_LN, operation.UF_PARAM_TL_NUM_FLUTES);

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
                                    s += string.Format("; ������ ����������={0:F2} ��.", operation.UF_PARAM_TL_COR1_RAD);
                                if (Math.Abs(operation.UF_PARAM_TL_TIP_ANG) > 0)
                                    s += string.Format("; ���� ��� �������={0:F2}<$s>", (90 - RadiansToDegrees(operation.UF_PARAM_TL_TIP_ANG) * 2));
                                if (Math.Abs(operation.UF_PARAM_TL_TAPER_ANG) > 0)
                                    s += string.Format("; ���� ������={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_TAPER_ANG) * 2);
                            }
                            break;

                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_BALL:
                            {
                                //s += string.Format("; ������ �����={0:F2} ��.", operation.UF_PARAM_TL_DIAMETER / 2);
                                s += string.Format("; ������ �����={0:F2} ��.", operation.UF_PARAM_TL_COR1_RAD);
                                if (Math.Abs(operation.UF_PARAM_TL_TAPER_ANG) > 0)
                                    s += string.Format("; ���� ������={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_TAPER_ANG) * 2);
                            }
                            break;

                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_CHAMFER:
                            if (Math.Abs(operation.UF_PARAM_TL_TAPER_ANG) > 0)
                                s += string.Format("; ���� �����={0:F2}<$s>", RadiansToDegrees(operation.UF_PARAM_TL_TAPER_ANG) * 2);
                            break;

                        case UFConstants.UF_CUTTER_SUBTYPE_MILL_SPHERICAL:
                            {
                                s += string.Format("; ������ �����={0:F2} ��.", operation.UF_PARAM_TL_DIAMETER / 2);
                                if (Math.Abs(operation.UF_PARAM_TL_SHANK_DIA) > 0)
                                    s += string.Format("; ������� �����={0:F2} ��.", RadiansToDegrees(operation.UF_PARAM_TL_SHANK_DIA) * 2);
                            }
                            break;
                    }
                    break;
                case UFConstants.UF_CUTTER_TYPE_BARREL:
                    {
                        if (Math.Abs(operation.UF_PARAM_TL_BARREL_RAD) > 0)
                            s += string.Format("; ������ �����={0:F2} ��.", operation.UF_PARAM_TL_BARREL_RAD);
                    }
                    break;
                case UFConstants.UF_CUTTER_TYPE_T:
                    {
                        if (Math.Abs(operation.UF_PARAM_TL_LOW_COR_RAD) > 0)
                            s += string.Format("; ������ ������ ����������={0:F2} ��.", operation.UF_PARAM_TL_LOW_COR_RAD);
                        if (Math.Abs(operation.UF_PARAM_TL_UP_COR_RAD) > 0)
                            s += string.Format("; ������� ������ ����������={0:F2} ��.", operation.UF_PARAM_TL_UP_COR_RAD);
                    }
                    break;
            }
            var strings = ParseDescriptionString(s);
            list.AddRange(strings.Select(s1 => new[] { string.Empty, "-", string.Empty, s1, string.Empty, string.Empty, string.Empty }));

            //-----------------------------------------------------

           if (operation.getCutComParams()) 
            {
                str = "�������� � �����������, ";
                str += operation.UF_PARAM_TL_CutcomOutputContactPoint ? "������ � ������� �������/������ �����������"
                    : "������ � ������� �������/������ = 0";
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