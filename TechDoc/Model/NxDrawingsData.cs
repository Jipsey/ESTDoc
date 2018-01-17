using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TechDocNS.Model
{
    public class NxDrawingsData
    {
        private NxSession _session;
        private List<KeyValuePair<string, string[]>> _machines;
        public string SelectedMachine;
        public string SelectedMachineGroup;
        public List<NxDrawingsFromat> DrawingsFromats;

        public NxDrawingsData(NxSession nxSession)
        {
            _session = nxSession;
        }

        public void InitializeData()
        {
            _machines = GetMachineNames();
            DrawingsFromats = GetDrawingsFromats();
        }

        public string[] MachineNames
        {
            get
            {
                return _machines != null ? _machines.Where(m => m.Key == SelectedMachineGroup).SelectMany(m => m.Value).ToArray() : new string[0];
            }
        }

        public string[] MachineGroup
        {
            get
            {
                return _machines != null ? _machines.Select(m => m.Key).ToArray() : new string[0];
            }
        }

        public List<KeyValuePair<string, string[]>> GetMachineNames()
        {
            try
            {
                var directories = Directory.GetDirectories(NxSession.ROOT_PATH_TXT, "станки", SearchOption.AllDirectories);
                var machinePath = directories.FirstOrDefault();

                if (string.IsNullOrEmpty(machinePath))
                    throw new Exception("Не удалось найти директорию с настройками станков!");

                return Directory.GetFiles(machinePath, "*.txt")
                    .Where(f => Path.GetFileName(f) != null && !Path.GetFileName(f).StartsWith("!"))
                    .Select(f =>
                        new KeyValuePair<string, string[]>(Path.GetFileNameWithoutExtension(f), File.ReadAllLines(f, Encoding.Default)
                            .Where(s => !string.IsNullOrEmpty(s)).ToArray())).ToList();
            }
            catch (DirectoryNotFoundException e)
            {
                throw new Exception("Не удалось найти директорию с настройками станков!");
            }
        }

        private List<NxDrawingsFromat> GetDrawingsFromats()
        {
            try
            {
                var directories = Directory.GetDirectories(NxSession.ROOT_PATH_TXT, "форматки", SearchOption.AllDirectories);
                var machinePath = directories.FirstOrDefault();
                
                if (string.IsNullOrEmpty(machinePath)) 
                    throw new Exception("Не удалось найти директорию с настройками форматок!");

                var list = Directory.GetFiles(machinePath, "*.txt")
                    .Where(f => !Path.GetFileName(f).StartsWith("!"))
                    .Select(f => File.ReadAllLines(f, Encoding.Default))
                    .SelectMany(l => l)
                    .Where(l => !l.StartsWith("#") && !string.IsNullOrEmpty(l))
                    .Select(l => l.Split(','))
                    .Where(arr => arr.Count() > 3)
                    .Select(arr => new NxDrawingsFromat(arr)).ToList();

                //            _session._session.ListingWindow.Open();
                //            list.ForEach(f => _session._session.ListingWindow.WriteFullline(f.Template + " | "));

                return list;
            }
            catch (DirectoryNotFoundException e)
            {
                throw new Exception("Не удалось найти директорию с настройками форматок!");
            }
        }
    }
}