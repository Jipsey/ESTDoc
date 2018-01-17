using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NXOpen;
using NXOpen.BlockStyler;
using TechDocNS.Model;

/*
    public static void Main()
  {
      TechDoc theTechDoc = null;
      try
      {
          theTechDoc = new TechDoc();
          theTechDoc._dataService = new DataService(theTechDoc);

          // The following method shows the dialog immediately
          theTechDoc.Show();
      }
      catch (Exception ex)
      {
          //---- Enter your exception handling code here -----
          theUI.NXMessageBox.Show("Block Styler", NXMessageBox.DialogType.Error, ex.ToString());
      }
      finally
      {
          if(theTechDoc != null)
              theTechDoc.Dispose();
              theTechDoc = null;
      }
  }
    
     
    public void initialize_cb()
  {
      _dataService.initialize_AttributeDataBinding();
  }    
  */


namespace TechDocNS.Services
{
    public class DataService
    {
        private NxSession _data;
        private TechDoc _dialog;
        private UI _ui;
        private Session _session;
        private ListingWindow _lw;
//        private NxToolsToDbUploaderService _uploaderService;

        public DataService(TechDoc theTechDoc)
        {
            _session = TechDoc.theSession;
            _ui = TechDoc.theUI;
            _dialog = theTechDoc;
            _lw = _session.ListingWindow;

            _data = new NxSession(_session, _ui);
//            _uploaderService = new NxToolsToDbUploaderService(_data);
        }

        public void initialize_AttributeDataBinding()
        {
            _data.InitializeAttribute();

            _data.Additional.RouteNumber = _dialog.integer0.Value;
            _data.Additional.OperationNumber = _dialog.integer01.Value;

            _dialog.string0.Value = _data.Attributes.PartNumber;//"TEST";//
            _dialog.string01.Value = _data.Attributes.PartName;
            _dialog.string02.Value = _data.Attributes.RazrabDataTime;
            _dialog.string03.Value = _data.Attributes.ProvDataTime;
            _dialog.string04.Value = _data.Attributes.UtverdDataTime;
            _dialog.string05.Enable = false;
            _dialog.string06.Value = _data.Attributes.CompanyName;

            // материал
            _dialog.enum0.SetEnumMembers(_data.Attributes.ListMaterial.ToArray());
            if (!string.IsNullOrEmpty(_data.Attributes.Material)) _dialog.enum0.ValueAsString = _data.Attributes.Material;

            // разработал
            _dialog.enum01.SetEnumMembers(_data.Attributes.ListRazrab.ToArray());
            if (!string.IsNullOrEmpty(_data.Attributes.Razrab)) _dialog.enum01.ValueAsString = _data.Attributes.Razrab;

            // проверил
            _dialog.enum02.SetEnumMembers(_data.Attributes.ListProv.ToArray());
            if (!string.IsNullOrEmpty(_data.Attributes.Prov)) _dialog.enum02.ValueAsString = _data.Attributes.Prov;

            // утвердил
            _dialog.enum03.SetEnumMembers(_data.Attributes.ListUtverd.ToArray());
            if (!string.IsNullOrEmpty(_data.Attributes.Utverd)) _dialog.enum03.ValueAsString = _data.Attributes.Utverd;

            // нормоконтроль
            _dialog.enum04.SetEnumMembers(_data.Attributes.ListNormokontr.ToArray());
            if (!string.IsNullOrEmpty(_data.Attributes.Normokontr)) _dialog.enum04.ValueAsString = _data.Attributes.Normokontr;
        }

        public void apply_AttributeDataBinding()
        {
            _data.Attributes.PartNumber = _dialog.string0.Value;
            _data.Attributes.PartName = _dialog.string01.Value;
            _data.Attributes.RazrabDataTime = _dialog.string02.Value;
            _data.Attributes.ProvDataTime = _dialog.string03.Value;
            _data.Attributes.UtverdDataTime = _dialog.string04.Value;
            _data.Attributes.CompanyName = _dialog.string06.Value;
            _data.Attributes.Material = _dialog.enum0.ValueAsString;
            _data.Attributes.Razrab = _dialog.enum01.ValueAsString;
            _data.Attributes.Prov = _dialog.enum02.ValueAsString;
            _data.Attributes.Utverd = _dialog.enum03.ValueAsString;
            _data.Attributes.Normokontr = _dialog.enum04.ValueAsString;
        }

        public void initialize_DrawingsDataBinding()
        {
            if (_data.Additional == null)
                _data.InitializeDrawingsData();

            initialize_cb(_dialog.enum05);
            initialize_cb(_dialog.enum06);
            initialize_cb(_dialog.enum07);
            initialize_cb(_dialog.enum09);
            initialize_cb(_dialog.enum011);
        }

        public void update_DrawingsDataBinding()
        {
            update_cb(_dialog.enum05);
            update_cb(_dialog.enum06);
            update_cb(_dialog.toggle0);
            update_cb(_dialog.toggle01);
            update_cb(_dialog.toggle02);
        }

        private void initialize_cb(UIBlock block)
        {
            // группа станков
            if (block == _dialog.enum05)
            {
                if (!_dialog.enum05.GetEnumMembers().Any())
                    _dialog.enum05.SetEnumMembers(_data.Additional.MachineGroup);
                if (!_dialog.enum05.GetEnumMembers().Any())
                    _ui.NXMessageBox.Show("Не получен список станков", NXMessageBox.DialogType.Error, "Не удалось получить список станков для работы! Проверьте директорию с настройками!");
                if (_dialog.enum05.GetEnumMembers().Any() && _dialog.enum05.GetEnumMembers().Contains(_data.Additional.SelectedMachineGroup))
                    _dialog.enum05.ValueAsString = _data.Additional.SelectedMachineGroup;
                else if (_dialog.enum05.GetEnumMembers().Any())
                    _dialog.enum05.ValueAsString = _data.Additional.MachineGroup.First();

                _dialog.enum05.Enable = _dialog.enum05.GetEnumMembers().Any();
            }
            else if (block == _dialog.enum06)
            {
                _dialog.enum06.SetEnumMembers(_data.Additional.MachineNames);

                if (_dialog.enum06.GetEnumMembers().Any() && !string.IsNullOrEmpty(_data.Additional.SelectedMachine) &&
                    _dialog.enum06.GetEnumMembers().Contains(_data.Additional.SelectedMachine))
                    _dialog.enum06.ValueAsString = _data.Additional.SelectedMachine;

                _dialog.enum06.Enable = _dialog.enum06.GetEnumMembers().Any();
                update_cb(_dialog.enum06);
            }
            else if (block == _dialog.enum07)
            {
                var strings = _data.Additional.DrawingsFromats.Where(df => df.DrawingType == 1 && df.IsFirstSheet).Select(df => df.ToString()).ToArray();
                _dialog.enum07.SetEnumMembers(strings);
                //_data.Drawings.CardsFormat.Where(f => f.Key == 1).Select(f => f.Value.ToString()).ToArray());
            }
            else if (block == _dialog.enum09)
            {
                var strings = _data.Additional.DrawingsFromats.Where(df => df.DrawingType == 2 && df.IsFirstSheet).Select(df => df.ToString()).ToArray();
                _dialog.enum09.SetEnumMembers(strings);
                //_data.Drawings.CardsFormat.Where(f => f.Key == 2).Select(f => f.Value.ToString()).ToArray());

            }
            else if (block == _dialog.enum011)
            {
                _dialog.enum011.SetEnumMembers(
                    //_data.Drawings.CardsFormat.Where(f => f.Key == 3).Select(f => f.Value.ToString()).ToArray());
                    _data.Additional.DrawingsFromats.Where(df => df.DrawingType == 3 && df.IsFirstSheet).Select(df => df.ToString()).ToArray());
            }
        }

        public void update_cb(UIBlock block)
        {
            // группа станков
            if (block == _dialog.enum05)
            {
                _data.Additional.SelectedMachineGroup = _dialog.enum05.ValueAsString;
                initialize_cb(_dialog.enum06);
            }
            // станок
            else if (block == _dialog.enum06)
            {
                _data.Additional.SelectedMachine = _dialog.enum06.ValueAsString;
                _dialog.group1.Enable = _dialog.enum06.Enable;
                //            _dialog.group2.Enable = _dialog.enum06.Enable;
                //            _dialog.group3.Enable = _dialog.enum06.Enable;
            }
            else if (block == _dialog.toggle0)
            {
                update_enable_gr(_dialog.group1.Members, false);//_dialog.toggle0.Value
                if (_dialog.toggle0.Value == true && !_data.TaggedObjects.Any())
                    _ui.NXMessageBox.Show("Не выбраны операции", NXMessageBox.DialogType.Warning, "Не забудьте выбрать одну или несколько программ в навигаторе операций!");

            }
            else if (block == _dialog.toggle01)
            {
                update_enable_gr(_dialog.group2.Members, _dialog.toggle01.Value);
            }
            else if (block == _dialog.toggle02)
            {
                update_enable_gr(_dialog.group3.Members, _dialog.toggle02.Value);
            }
            else if (block == _dialog.button0)
            {
                OpenHelpFileInLw("по_атрибутам");
            }
            else if (block == _dialog.button01)
            {
                OpenHelpFileInLw("о_программе");
            }
            else if (block == _dialog.button05)
            {
                OpenHelpFileInLw("по_чертежам");
            }
//            else if (block == _dialog.button06)
//            {
//                OpenHelpFileInLw("по_инструментам");
//           }
            else if (block == _dialog.button02)
            {
                var toolsCardAttributesFilter = NxSession.GetToolsCardAttributesFilter();
                if (toolsCardAttributesFilter == null || !toolsCardAttributesFilter.Any() || _lw == null) return;
                _lw.Open();
                toolsCardAttributesFilter.ForEach(l => _lw.WriteFullline(l));
            }
            else if (block == _dialog.button03)
            {
                var openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = "Текстовые файлы |*.txt",
                    Title = "Укажите файл с тех.требованиями для карты наладки"
                };

                var f = NxSession.GetDirectory("карта_наладки");
                if (!string.IsNullOrEmpty(f)) openFileDialog.InitialDirectory = f;

                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                _data.SETUP_CARD_FILE_TT = openFileDialog.FileName;
            }
            else if (block == _dialog.button04)
            {
                var openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Filter = "Текстовые файлы |*.txt",
                    Title = "Укажите файл с тех.требованиями для карты эскизов"
                };

                var f = NxSession.GetDirectory("карта_эскизов");
                if (!string.IsNullOrEmpty(f)) openFileDialog.InitialDirectory = f;

                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                _data.SKETCH_CARD_FILE_TT = openFileDialog.FileName;
            }
            else if (block == _dialog.integer0)
            {
                _data.Additional.RouteNumber = ((IntegerBlock) block).Value;
            }
            else if (block == _dialog.integer01)
            {
                _data.Additional.OperationNumber = ((IntegerBlock) block).Value;
            }
        }

        private void OpenHelpFileInLw(string s)
        {

            if (_lw == null) return;
            if (!_lw.IsOpen) _lw.Open();

            var helpList = _data.GetHelpList(s);
            if (helpList.Any())
                if (s.Equals("о_программе"))
                helpList.Add(helpAdded);
                helpList.ForEach(l => _lw.WriteFullline(l));
        }

        private void update_enable_gr(PropertyList members, bool value)
        {
            for (var i = 0; i < members.Length; i++)
            {
                var uiBlock = members.GetUIBlock(i);
                if (uiBlock is Enumeration || uiBlock is IntegerBlock) uiBlock.Enable = value;
            }
        }

        /*
        public bool get_EnableApplyData()
        {
            return _dialog.tabControl.ActivePage != 1 || _data.TaggedObjects.Any();
        }
    */

        /*
        public void get_SelectedObjects()
        {
            if(_dialog.tabControl.ActivePage != 1) return;
            _dialog.tabPage1.Enable = _data.TaggedObjects.Any();
            if (!_data.TaggedObjects.Any() && _ui != null)
                _ui.NXMessageBox.Show("Не выбраны операции", NXMessageBox.DialogType.Error,"Необходимо выбрать операцию, либо группу операций!");
        }
    */

        public void apply_DrawingsDataBinding()
        {
            try
            {
                _data.GetSelectedObjects();
                if (!_data.TaggedObjects.Any())
                {
                    const string s = "Необходимо выбрать операцию, либо группу операций!";
                    //throw new Exception(s);
                    if (_ui != null) _ui.NXMessageBox.Show("Не выбраны операции", NXMessageBox.DialogType.Error, s);
                    return;
                }
                
                _data.GetNxOperationGroups();
                
                if (string.IsNullOrEmpty(_dialog.enum06.ValueAsString))
                {
                    const string s = "Необходимо выбрать один из станков!";
                    //                    throw new Exception(s);
                    if (_ui != null) _ui.NXMessageBox.Show("Не выбраны станки", NXMessageBox.DialogType.Error, s);
                    return;
                }
                
                var drawingsSetups = new List<DrawingsSetup>();

                drawingsSetups.Add(new DrawingsSetup
                {
                    DrawingsType = 0,
                    AdditionalToolGroupName = _data.Additional.SelectedMachineGroup,
                    AdditionalToolName = _data.Additional.SelectedMachine
                });

                if (_dialog.toggle0.Value)
                {
                    drawingsSetups.Add(new DrawingsSetup
                    {
                        DrawingsType = 1,
                        DrawingsFormatName = _dialog.enum07.ValueAsString,
                        SheetNums = _dialog.integer02.Value,
                        OperationNumber = _dialog.integer01.Value
                    });
                }

                if (_dialog.toggle01.Value)
                {
                    drawingsSetups.Add(new DrawingsSetup
                    {
                        DrawingsType = 2,
                        DrawingsFormatName = _dialog.enum09.ValueAsString,
                        SheetNums = _dialog.integer021.Value,
                        OperationNumber = _dialog.integer01.Value
                    });

                    //                    _data.CreateDrawings(sheetSetup);
                }

                if (_dialog.toggle02.Value)
                {
                    drawingsSetups.Add(new DrawingsSetup
                    {
                        DrawingsType = 3,
                        DrawingsFormatName = _dialog.enum011.ValueAsString,
                        SheetNums = _dialog.integer022.Value,
                        OperationNumber = _dialog.integer01.Value
                    });
                }


                _data.CreateDrawings(drawingsSetups);

                _dialog.integer01.Value += (int)_dialog.integer01.Increment;

                UI.GetUI().NXMessageBox.Show("Внимание", NXMessageBox.DialogType.Information, "Технологическая документация успешно создана.");

            }
            catch (Exception e)
            {
                if(e.Message != string.Empty)
                   UI.GetUI().NXMessageBox.Show("Ошибка построения карт наладки", NXMessageBox.DialogType.Error, e.Message);
                else
                   UI.GetUI().NXMessageBox.Show("Ошибка построения карт наладки", NXMessageBox.DialogType.Error, e.ToString());
            }
        }

        //UI.GetUI().NXMessageBox.Show("Ошибка построения карт наладки", NXMessageBox.DialogType.Warning, e.ToString());

        string helpAdded = "--------------------------------------------------------------------------------\n" +
"ИНФОРМАЦИЯ ПО ВЕРСИИ 3.0.Бета для версии NX 11.0. \n" +
         " \n" +
"Что нового:\n" +
        "\n" +
"Переработан и улучшен вывод информации в КИ:\n" +
"- исправлены ошибки с выводом описания и нумерции технологической документации; \n" +
"- устранена ошибка кодировки при выводе символов кирилицы в КИ;  \n" +
"- добавлен вывод в КИ номера(-ов) регистра(-ов) коррекции токарных инструментов, с указанием \n" +
"  угловой ориентации и поворота вокруг своей оси; \n" +
"- введана возможность корректного вывода диамтера фреза специальной формы; \n" +
"- исправлен вывод радиуса полнорадиусной пластины; \n" +
"- добавлен вывод в КИ значения зоны досягаемости инструмента \n" +
"  (при наличии значения в соответсвующем поле); \n"+
"- добавлен вывод информации в КИ работает ли инструмент с компенсацией радиуса/диаметра; \n" +
"- добавлена проверка на эквивалентность вывода точки трассировки для инструмента, при включённой коррекции; \n" +
"\n"+
"жалобы и предложения по работе данной версии приложения направлять на электронную почту: \n" +
"mailforexist@yandex.ru автор версии Васильчук Александр. \n" +
"--------------------------------------------------------------------------------\n";

            
        public void deleteTempOperation() {

     
//            Session.UndoMarkId markId1 = NxSession.Session.SetUndoMark(NXOpen.Session.MarkVisibility.Visible, "Delete");

   
//            int nErrs1 = NxSession.Session.UpdateManager.AddToDeleteList(NxSession.arrCAMObj);

//            int nErrs2 = NxSession.Session.UpdateManager.DoUpdate(markId1);
        }

        public void apply_UploadToolsToDataBase()
        {
//            if (!_dialog.toggle03.Value) return;

//            if(MessageBox.Show("Начать выгрузку инструмента в базу?", "", MessageBoxButtons.YesNo) == DialogResult.No) return;

//            try
//            {
//                _data.GetNxOperationGroups();
//                _uploaderService.UploadNxToolsToDb();
//            }
//            catch (Exception e)
//            {
//                UI.GetUI().NXMessageBox.Show("Ошибка выгрузки инструмента в базу!", NXMessageBox.DialogType.Error, e.ToString());
//            }
//            MessageBox.Show("Выгрузка инструмента закончена.");
        }
    }
}