using System;
using System.Linq;

namespace TechDocNS.Model
{
    public class NxDrawingsFromat
    {
        /*
        # �������� �������� ��� ������ ���� ������������, �������, �������
        # �������� �������� � ��������� �������:
        #  <��� �����>,<�������� ��� ��� ������ � �������>,<��� ����� � �������� ��������>,<������� �����>
        #
        # ���� ����:
        # 1 - ����� �����������
        # 2 - ����� �������
        # 3 - ����� �������
        #
        # ������� �����:
        # 1 - ������ ��� ������� �����
        # 2 - ������ ��� ����������� ������
        #
        # ��������: A4, diakont_template_A4_GOST_3.1404-84_form_4.prt, 1, 1
        */

        public string Name;
        public string Template;
        public int DrawingType;
        private int SheetType;

        public NxDrawingsFromat(string[] arr)
        {
            int n1, n2;
            if (arr.Count() < 4 || !int.TryParse(arr[0], out n1) || !int.TryParse(arr[3], out n2))
                throw new Exception("������ ������ ����� � ��������� �������� ������!");

            DrawingType = n1;
            SheetType = n2;
            Name = arr[1].Trim();
            Template = arr[2].Trim();
        }

        public bool IsFirstSheet { get { return SheetType == 1; } }

        public override string ToString()
        {
            return Name;
        }
    }
}