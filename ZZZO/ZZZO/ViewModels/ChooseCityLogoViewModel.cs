using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using ZZZO.Common;

namespace ZZZO.ViewModels;

public class CityLogo : ObservableObject
{
  #region Proměnné

  private string _cityName;
  private string _extendedCityClusterName;

  private BitmapImage _logoObce;

  #endregion

  #region Vlastnosti

  public string CityName
  {
    get => _cityName;
    set
    {
      if (value == _cityName)
      {
        return;
      }

      _cityName = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(FullName));
    }
  }

  public string ExtendedCityClusterName
  {
    get => _extendedCityClusterName;
    set
    {
      if (value == _extendedCityClusterName)
      {
        return;
      }

      _extendedCityClusterName = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(FullName));
    }
  }

  public string FullName
  {
    get => $"{CityName} ({ExtendedCityClusterName})";
  }

  public BitmapImage LogoObce
  {
    get => _logoObce;
    set
    {
      if (Equals(value, _logoObce))
      {
        return;
      }

      _logoObce = value;
      OnPropertyChanged();
    }
  }

  public byte[] LogoObceData
  {
    get
    {
      if (LogoObce == null)
      {
        return null;
      }

      PngBitmapEncoder encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(LogoObce));

      using (MemoryStream ms = new MemoryStream())
      {
        encoder.Save(ms);
        return ms.ToArray();
      }
    }

    set
    {
      if (value == null || value.Length == 0)
      {
        LogoObce = null;
        return;
      }

      using (MemoryStream stream = new MemoryStream(value))
      {
        BitmapImage bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = stream;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        LogoObce = bitmap;
      }
    }
  }

  #endregion
}

public class ChooseCityLogoViewModel : ViewModelBase
{
  #region Proměnné

  private ObservableCollection<CityLogo> _loga;
  private CityLogo _logo;

  #endregion

  #region Vlastnosti

  public ObservableCollection<CityLogo> Loga
  {
    get => _loga;
    set
    {
      if (Equals(value, _loga))
      {
        return;
      }

      _loga = value;
      OnPropertyChanged();
    }
  }

  public ChooseCityLogoViewModel()
  {
    Loga = new ObservableCollection<CityLogo>
    {
      new CityLogo
      {
        CityName = "Svésedlice",
        ExtendedCityClusterName = "Příslavice",
        LogoObce = new BitmapImage(new Uri("https://rekos.psp.cz/data/images/39913/200x200/bohuslavice0003.jpg"))
      },
      new CityLogo
      {
        CityName = "CojaVim",
        ExtendedCityClusterName = "Příslavice",
        LogoObce = new BitmapImage(new Uri("https://rekos.psp.cz/data/images/34392/200x200/bohuslavice_prostejov.jpg"))
      }
    };
  }

  public CityLogo Logo
  {
    get => _logo;
    set
    {
      if (Equals(value, _logo))
      {
        return;
      }

      _logo = value;
      OnPropertyChanged();
    }
  }

  #endregion
}