using System;
using System.Linq;
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


public class DataService
{
    private NxSession _data;
    private TechDoc _dialog;
    private UI _ui;
    private string _machineGroupFilter;
    private int[] initialShortcuts;

    public DataService(TechDoc theTechDoc)
    {
        _data = new NxSession(TechDoc.theSession);
        _ui = TechDoc.theUI;
        _dialog = theTechDoc;
    }

    public void initialize_AttributeDataBinding()
    {
        _data.InitializeAttribute();

        _dialog.string0.Value = "TEST";//_data.Attributes.PartNumber;
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

    public void initialize_DrawingsDataBinding()
    {
        if (_data.Drawings == null)
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
                _dialog.enum05.SetEnumMembers(_data.Drawings.MachineGroup);
            if (!_dialog.enum05.GetEnumMembers().Any())
                _ui.NXMessageBox.Show("Не получен список станков", NXMessageBox.DialogType.Error,"Не удалось получить список станков для работы! Проверьте директорию с настройками!");
            if(_dialog.enum05.GetEnumMembers().Any() &&_dialog.enum05.GetEnumMembers().Contains(_data.Drawings.SelectedMachineGroup))
                _dialog.enum05.ValueAsString = _data.Drawings.SelectedMachineGroup;
            else if(_dialog.enum05.GetEnumMembers().Any())
                _dialog.enum05.ValueAsString = _data.Drawings.MachineGroup.First();

            _dialog.enum05.Enable = _dialog.enum05.GetEnumMembers().Any();
        }
        else if (block == _dialog.enum06)
        {
            _dialog.enum06.SetEnumMembers(_data.Drawings.MachineNames);

            if (_dialog.enum06.GetEnumMembers().Any() && !string.IsNullOrEmpty(_data.Drawings.SelectedMachine) &&
                _dialog.enum06.GetEnumMembers().Contains(_data.Drawings.SelectedMachine))
                _dialog.enum06.ValueAsString = _data.Drawings.SelectedMachine;
            
            _dialog.enum06.Enable = _dialog.enum06.GetEnumMembers().Any();
            update_cb(_dialog.enum06);
        }
        else if (block == _dialog.enum07)
        {
            _dialog.enum07.SetEnumMembers(
                _data.Drawings.CardsFormat.Where(f => f.Key == 1).Select(f => f.Value.ToString()).ToArray());
        }
        else if (block == _dialog.enum09)
        {
            _dialog.enum09.SetEnumMembers(
                _data.Drawings.CardsFormat.Where(f => f.Key == 2).Select(f => f.Value.ToString()).ToArray());
        }
        else if (block == _dialog.enum011)
        {
            _dialog.enum011.SetEnumMembers(
                _data.Drawings.CardsFormat.Where(f => f.Key == 3).Select(f => f.Value.ToString()).ToArray());
        }
    }

    public void update_cb(UIBlock block)
    {
        // группа станков
        if (block == _dialog.enum05)
        {
            _data.Drawings.SelectedMachineGroup = _dialog.enum05.ValueAsString;
            
            initialize_cb(_dialog.enum06);
        }
        // станок
        else if (block == _dialog.enum06)
        {
            _data.Drawings.SelectedMachine = _dialog.enum06.ValueAsString;
            _dialog.group1.Enable = _dialog.enum06.Enable;
            _dialog.group2.Enable = _dialog.enum06.Enable;
            _dialog.group3.Enable = _dialog.enum06.Enable;
        }
        else if (block == _dialog.toggle0)
        {
            update_enable_gr(_dialog.group1.Members, _dialog.toggle0.Value);
        }
        else if (block == _dialog.toggle01)
        {
            update_enable_gr(_dialog.group2.Members, _dialog.toggle01.Value);
        }
        else if (block == _dialog.toggle02)
        {
            update_enable_gr(_dialog.group3.Members, _dialog.toggle02.Value);
        }
    }

    private void update_enable_gr(PropertyList members, bool value)
    {
        for (var i = 0; i < members.Length; i++)
        {
            var uiBlock = members.GetUIBlock(i);
            if (uiBlock is Enumeration || uiBlock is IntegerBlock) uiBlock.Enable = value;
        }
    }
}