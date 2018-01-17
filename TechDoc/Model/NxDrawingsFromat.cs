using System;
using System.Linq;

namespace TechDocNS.Model
{
    public class NxDrawingsFromat
    {
        /*
        # Описание форматок для вывода карт инструментов, наладки, эксизов
        # Форматки задавать в следующем порядке:
        #  <Тип карты>,<Короткое имя для списка в диалоге>,<Имя файла с шаблоном форматки>,<Порядок листа>
        #
        # Типы карт:
        # 1 - Карта инструмента
        # 2 - Карта наладки
        # 3 - Карта эскизов
        #
        # Порядок листа:
        # 1 - формат для первого листа
        # 2 - формат для последующих листов
        #
        # Например: A4, diakont_template_A4_GOST_3.1404-84_form_4.prt, 1, 1
        */

        public string Name;
        public string Template;
        public int DrawingType;
        private int SheetType;

        public NxDrawingsFromat(string[] arr)
        {
            int n1, n2;
            if (arr.Count() < 4 || !int.TryParse(arr[0], out n1) || !int.TryParse(arr[3], out n2)) 
                throw new Exception("Ошибка чтения файла с описанием форматок листов!");

            DrawingType = n1;
            SheetType = n2;
            Name = arr[1].Trim();
            Template = arr[2].Trim();
        }

        public bool IsFirstSheet { get { return SheetType == 1; }}

        public override string ToString()
        {
            return Name;
        }
    }
}