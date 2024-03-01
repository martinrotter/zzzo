using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using ZZZO.Commands;
using ZZZO.Common;
using ZZZO.Common.API;
using ZZZO.Controls;

namespace ZZZO.ViewModels;

public class BasicInfoViewModel : ViewModelBase
{
  #region Proměnné

  private ZzzoCore _core;

  private Zastupitel _chosenZastupitel;

  #endregion

  #region Vlastnosti

  public ICommand AddZastupitelCmd
  {
    get;
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

  public ICommand RemoveVillageLogoCmd
  {
    get;
  }

  public ICommand RemoveZastupitelCmd
  {
    get;
  }

  public ICommand ShowCityLogosCmd
  {
    get;
  }

  public ICommand UpdateVillageLogoCmd
  {
    get;
  }

  public ObservableCollection<Zastupitel> Zastupitele
  {
    get => Core?.Zasedani?.Zastupitele;
  }

  #endregion

  #region Konstruktory

  public BasicInfoViewModel(ZzzoCore core)
  {
    Core = core;

    Core.PropertyChanged += (sender, args) =>
    {
      if (args.PropertyName == nameof(Zasedani))
      {
        OnPropertyChanged(nameof(Zastupitele));

        if (Zastupitele != null)
        {
          Zastupitele.CollectionChanged += (o, eventArgs) =>
          {
            if (eventArgs.Action != NotifyCollectionChangedAction.Add || eventArgs.NewItems ==null)
            {
              return;
            }

            foreach (Zastupitel newZastupitel in eventArgs.NewItems)
            {
              newZastupitel.PropertyChanged += ReagovatNaZmenyExkluzivnichPropertyZastupitele;
            }
          };

          foreach (Zastupitel zastupitel in Zastupitele)
          {
            zastupitel.PropertyChanged += ReagovatNaZmenyExkluzivnichPropertyZastupitele;
          }
        }
      }
    };

    UpdateVillageLogoCmd = new RelayCommand(obj => UpdateVillageLogo(), obj => true);
    ShowCityLogosCmd = new RelayCommand<string>(str => ShowCityLogos(str), obj => true);
    RemoveVillageLogoCmd = new RelayCommand(obj => RemoveVillageLogo(), obj => Core?.Zasedani?.LogoObce != null);
    AddZastupitelCmd = new RelayCommand(obj => AddZastupitel(), obj => true);
    RemoveZastupitelCmd = new RelayCommand(obj => RemoveZastupitel(), obj => ChosenZastupitel != null);
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

    if (Core.Zasedani.Zastupitele.Count == 1)
    {
      ChosenZastupitel = Core.Zasedani.Zastupitele[0];
    }
  }

  private void ReagovatNaZmenyExkluzivnichPropertyZastupitele(object sender, PropertyChangedEventArgs e)
  {
    List<string> exkluzivniProperies = new List<string>
    {
      nameof(Zastupitel.JeStarosta),
      nameof(Zastupitel.JeRidici),
      nameof(Zastupitel.JeZapisovatel)
    };

    if (exkluzivniProperies.Contains(e.PropertyName) && sender.GetPropertyValue<bool>(e.PropertyName))
    {
      Utils.UpdateExclusiveProperty(sender, Zastupitele, e.PropertyName);
    }
  }

  private void RemoveVillageLogo()
  {
    Core.Zasedani.LogoObce = null;
  }

  private void RemoveZastupitel()
  {
    if (ChosenZastupitel != null)
    {
      Core.Zasedani.RemoveZastupitel(ChosenZastupitel);
    }
  }

  private void ShowCityLogos(string cityName)
  {
    DialogHost.Show(new ChooseCityLogo
    {
      DataContext = new ChooseCityLogoViewModel(Core, cityName)
    }, (o, a) =>
    {
      if (a.Parameter is CityLogo cl)
      {
        Core.Zasedani.LogoObce = cl.LogoObce;
      }
    });
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