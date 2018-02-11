using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NXOpen;

namespace TechDocNS.Model
{
    public class NxAttribute
    {
        private readonly Part _part1;
        private NxSession _session;
        private List<string> _listMaterial;
        private List<string> _listRazrab;
        private List<string> _listProv;
        private List<string> _listUtverd;
        private List<string> _listNorm;
        private string _currentDateTime;

        public NxAttribute(NxSession session)
        {
            if (session == null) throw new Exception("Ñåññèÿ NX íå çàïóùåíà!");
            _session = session;
            _part1 = NxSession.Part;
            _currentDateTime = DateTime.Today.ToString("d");
        }

        public List<string> ListMaterial
        {
            get { if (!String.IsNullOrEmpty(Material) && !_listMaterial.Contains(Material))_listMaterial.Add(Material); return _listMaterial; }
            set { _listMaterial = value; }
        }

        public List<string> ListRazrab
        {
            get { if (!String.IsNullOrEmpty(Razrab) && !_listRazrab.Contains(Razrab))_listRazrab.Add(Razrab); return _listRazrab; }
            set { _listRazrab = value; }
        }

        public List<string> ListProv
        {
            get { if (!String.IsNullOrEmpty(Prov) && !_listProv.Contains(Prov))_listProv.Add(Prov); return _listProv; }
            set { _listProv = value; }
        }

        public List<string> ListUtverd
        {
            get { if (!String.IsNullOrEmpty(Utverd) && !_listUtverd.Contains(Utverd))_listUtverd.Add(Utverd); return _listUtverd; }
            set { _listUtverd = value; }
        }

        public List<string> ListNormokontr
        {
            get { if (!String.IsNullOrEmpty(Normokontr) && !_listUtverd.Contains(Normokontr)) _listUtverd.Add(Normokontr); return _listNorm; }
            set { _listNorm = value; }
        }

        private Part _Part
        {
            get { if (_part1 == null) throw new Exception("Íå íàéäåíà îòêğûòàÿ äåòàëü!"); return _part1; }
        }

        public string PartNumber
        {
            get { return (string)GetAttribute("ÍÎÌÅĞ_ÄÅÒÀËÈ"); }
            set { SetAttribute("ÍÎÌÅĞ_ÄÅÒÀËÈ", value); }
        }

        public string PartName
        {
            get { return (string)GetAttribute("ÈÌß_ÄÅÒÀËÈ"); }
            set { SetAttribute("ÈÌß_ÄÅÒÀËÈ", value); }
        }

        public string Material
        {
            get { return (string)GetAttribute("ÌÀÒÅĞÈÀË_ÄÅÒÀËÈ"); }
            set { SetAttribute("ÌÀÒÅĞÈÀË_ÄÅÒÀËÈ", value); }
        }


        public string Razrab
        {
            get { return (string)GetAttribute("ĞÀÇĞÀÁÎÒÀË"); }
            set { SetAttribute("ĞÀÇĞÀÁÎÒÀË", value); }
        }


        public string Prov
        {
            get { return (string)GetAttribute("ÏĞÎÂÅĞÈË"); }
            set { SetAttribute("ÏĞÎÂÅĞÈË", value); }
        }

        public string Utverd
        {
            get { return (string)GetAttribute("ÓÒÂÅĞÄÈË"); }
            set { SetAttribute("ÓÒÂÅĞÄÈË", value); }
        }

        public string Normokontr
        {
            get { return (string)GetAttribute("ÍÎĞÌÎÊÎÍÒĞÎËÜ"); }
            set { SetAttribute("ÍÎĞÌÎÊÎÍÒĞÎËÜ", value); }
        }

        public string RazrabDataTime
        {
            get
            {
                var s = (string)GetAttribute("ÄÀÒÀ_ĞÀÇĞÀÁÎÒÊÈ");
                return !string.IsNullOrEmpty(s) ? s : _currentDateTime;
            }
            set { SetAttribute("ÄÀÒÀ_ĞÀÇĞÀÁÎÒÊÈ", value); }
        }

        public string ProvDataTime
        {
            get
            {
                var s = (string)GetAttribute("ÄÀÒÀ_ÏĞÎÂÅĞÊÈ");
                return !string.IsNullOrEmpty(s) ? s : _currentDateTime;
            }
            set { SetAttribute("ÄÀÒÀ_ÏĞÎÂÅĞÊÈ", value); }
        }

        public string UtverdDataTime
        {
            get
            {
                var s = (string)GetAttribute("ÄÀÒÀ_ÓÒÂÅĞÆÄÅÍÈß");
                return !string.IsNullOrEmpty(s) ? s : _currentDateTime;
            }
            set { SetAttribute("ÄÀÒÀ_ÓÒÂÅĞÆÄÅÍÈß", value); }
        }

        public string CompanyName
        {
            get
            {
                var s = (string)GetAttribute("ÈÌß_ÊÎÌÏÀÍÈÈ");
                //return !string.IsNullOrEmpty(s) ? s : "ÀÎ \"Äèàêîíò\" ";
                return " ËåíÒóğáîĞåìîíò ";
            }
            set { SetAttribute("ÈÌß_ÊÎÌÏÀÍÈÈ", value); }
        }

        public void GetAttributeFromFiles()
        {
            var filePath = NxSession.ROOT_PATH_TXT;
            if (string.IsNullOrEmpty(filePath)) throw new Exception("Íå óäàëîñü íàéòè äèğåêòîğèş ñ òåêñòîâûìè ôàéëàìè íàñòğîéêè!");

            ListMaterial = GetListFromFiles(Directory.GetFiles(filePath, "ìàòåğèàë*.txt", SearchOption.AllDirectories));
            ListRazrab = GetListFromFiles(Directory.GetFiles(filePath, "ğàçğàáîòàë.txt", SearchOption.AllDirectories));
            ListProv = GetListFromFiles(Directory.GetFiles(filePath, "ïğîâåğèë.txt", SearchOption.AllDirectories));
            ListUtverd = GetListFromFiles(Directory.GetFiles(filePath, "óòâåğäèë.txt", SearchOption.AllDirectories));
            ListNormokontr = GetListFromFiles(Directory.GetFiles(filePath, "íîğìîêîíòğîëü.txt", SearchOption.AllDirectories));
        }

        private static List<string> GetListFromFiles(string[] files)
        {
            return files.Any()
                ? files.Select(f => File.ReadAllLines(f, Encoding.Default).Where(s => !String.IsNullOrEmpty(s)).Select(s => s.Split(',').First())).SelectMany(l => l).ToList()
                : new List<string>();
        }


        private object GetAttribute(string attName, NXObject.AttributeType attType = NXObject.AttributeType.String, int attArrayNumber = -1)
        {
            if (_Part == null || !_Part.HasUserAttribute(attName, attType, attArrayNumber)) return null;

            var attribute = _Part.GetUserAttribute(attName, attType, 1);
            switch (attType)
            {
                case NXObject.AttributeType.String:
                    return attribute.StringValue;

                case NXObject.AttributeType.Time:
                    return attribute.TimeValue;

                case NXObject.AttributeType.Integer:
                    return attribute.IntegerValue;

                case NXObject.AttributeType.Boolean:
                    return attribute.BooleanValue;

                default:
                    return null;
            }
        }

        private void SetAttribute(string attName, object attValue, NXObject.AttributeType attType = NXObject.AttributeType.String)
        {
            if (_Part == null) return;

            switch (attType)
            {
                case NXObject.AttributeType.String:
                    _Part.SetUserAttribute(attName, -1, (string)attValue, Update.Option.Later);
                    break;

                case NXObject.AttributeType.Time:
                    _Part.SetUserAttribute(attName, -1, (string)attValue, Update.Option.Later);
                    break;

                case NXObject.AttributeType.Integer:
                    _Part.SetUserAttribute(attName, -1, (int)attValue, Update.Option.Later);
                    break;

                case NXObject.AttributeType.Boolean:
                    _Part.SetUserAttribute(attName, -1, (int)attValue, Update.Option.Later);
                    break;

                default:
                    break;
            }
        }

        private static List<string> GetAttributeFilter(string filename)
        {
            var ret = new List<string>();
            if (File.Exists(filename)) ret.AddRange(File.ReadAllLines(filename));
            return ret;
        }
    }
}