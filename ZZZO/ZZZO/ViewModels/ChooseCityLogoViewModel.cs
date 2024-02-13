using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ZZZO.Commands;
using ZZZO.Common;

namespace ZZZO.ViewModels;

public class CityLogo : ObservableObject
{
  #region Proměnné

  private string _cityName;
  private string _extendedCityClusterName;

  private BitmapImage _logoObce;
  private string _logoObceUrl;

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

  public string LogoObceUrl
  {
    get => _logoObceUrl;
    set
    {
      if (value == _logoObceUrl)
      {
        return;
      }

      _logoObceUrl = value;
      OnPropertyChanged();
    }
  }

  #endregion
}

public class ChooseCityLogoViewModel : ViewModelBase
{
  #region Proměnné

  private string _error;

  private ObservableCollection<CityLogo> _loga;
  private CityLogo _logo;
  private bool _logosDownloaded;
  private bool _logosDownloading;

  #endregion

  #region Vlastnosti

  public string CityName
  {
    get;
  }

  public ZzzoCore Core
  {
    get;
  }

  public string Error
  {
    get => _error;
    set
    {
      if (value == _error)
      {
        return;
      }

      _error = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(HasError));
    }
  }

  public bool HasError
  {
    get => !string.IsNullOrEmpty(Error);
  }

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

      if (_logo != null && !string.IsNullOrEmpty(_logo.LogoObceUrl) && _logo.LogoObce == null)
      {
        Core.DownloadFullCityLogo(_logo).Wait();
      }

      OnPropertyChanged();
    }
  }

  public bool LogosDownloaded
  {
    get => _logosDownloaded;
    set
    {
      if (value == _logosDownloaded)
      {
        return;
      }

      _logosDownloaded = value;
      OnPropertyChanged();
    }
  }

  public bool LogosDownloading
  {
    get => _logosDownloading;
    set
    {
      if (value == _logosDownloading)
      {
        return;
      }

      _logosDownloading = value;
      OnPropertyChanged();
    }
  }

  public ICommand StahnoutLogosCmd
  {
    get;
  }

  #endregion

  #region Konstruktory

  public ChooseCityLogoViewModel(ZzzoCore core, string cityName)
  {
    Core = core;
    CityName = cityName;
    StahnoutLogosCmd = new RelayCommand(o => StahnoutLogos(), o => true);

    Task.Delay(100).ContinueWith(tsk => { StahnoutLogosCmd.Execute(null); }, TaskScheduler.FromCurrentSynchronizationContext());
  }

  #endregion

  #region Metody

  private void StahnoutLogos()
  {
    LogosDownloading = true;

    Core.DownloadCityLogos(CityName)
      .ContinueWith(tsk =>
      {
        Loga = new ObservableCollection<CityLogo>(tsk.Result);
        Logo = Loga.FirstOrDefault();
        LogosDownloading = false;
        LogosDownloaded = Loga != null;
        Error = tsk.Exception?.Message;
      }, TaskScheduler.FromCurrentSynchronizationContext());
  }

  #endregion
}