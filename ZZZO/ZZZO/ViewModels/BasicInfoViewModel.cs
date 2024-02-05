using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using ZZZO.Commands;
using ZZZO.Common.API;

namespace ZZZO.ViewModels;

public class BasicInfoViewModel : ViewModelBase
{
  #region Proměnné

  private Zastupitel _chosenZastupitel;

  private ZzzoCore _core;

  #endregion

  #region Vlastnosti

  public ICommand AddZastupitelCmd
  {
    get;
  }

  public Zastupitel ChosenZastupitel
  {
    get => _chosenZastupitel;
    set
    {
      if (Equals(value, _chosenZastupitel))
      {
        return;
      }

      _chosenZastupitel = value;
      OnPropertyChanged();
    }
  }

  public ZzzoCore Core
  {
    get => _core;
    set
    {
      if (Equals(value, _core))
      {
        return;
      }

      _core = value;
      OnPropertyChanged();
    }
  }

  public ICommand RemoveZastupitelCmd
  {
    get;
  }

  public ICommand UpdateVillageLogoCmd
  {
    get;
  }

  #endregion

  #region Konstruktory

  public BasicInfoViewModel(ZzzoCore core)
  {
    Core = core;

    UpdateVillageLogoCmd = new RelayCommand(obj => UpdateVillageLogo(), obj => true);
    AddZastupitelCmd = new RelayCommand(obj => AddZastupitel(), obj => true);
    RemoveZastupitelCmd = new RelayCommand(RemoveZastupitel, obj => ChosenZastupitel != null);
  }

  #endregion

  #region Metody

  private void AddZastupitel()
  {
    Core.Zasedani.AddZastupitel(new Zastupitel
    {
      Jmeno = "Nový",
      Prijmeni = "Zastupitel"
    });
  }

  private void RemoveZastupitel(object arg)
  {
    if (ChosenZastupitel != null)
    {
      Core.Zasedani.RemoveZastupitel(ChosenZastupitel);
    }
  }

  private void UpdateVillageLogo()
  {
    OpenFileDialog d = new OpenFileDialog();

    d.AddExtension = true;
    d.CheckPathExists = true;
    d.Filter = "Obrázky (BMP, PNG, JPG)|*.bmp;*.png;*.jpg;*.jpeg";
    d.Title = "Zvolte lokaci pro načtení loga ze souboru";

    if (d.ShowDialog().GetValueOrDefault())
    {
      Core.Zasedani.LogoObceData = File.ReadAllBytes(d.FileName);
    }
  }

  #endregion
}