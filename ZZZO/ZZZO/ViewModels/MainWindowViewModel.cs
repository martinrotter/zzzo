using ZZZO.Common.API;

namespace ZZZO.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  #region Proměnné

  private App _app;
  private Zasedani _zasedani;

  #endregion

  #region Vlastnosti

  public App App
  {
    get => _app;
    set
    {
      if (Equals(value, _app))
      {
        return;
      }

      _app = value;
      OnPropertyChanged();
    }
  }

  public Zasedani Zasedani
  {
    get => _zasedani;
    set
    {
      if (Equals(value, _zasedani))
      {
        return;
      }

      _zasedani = value;
      OnPropertyChanged();
    }
  }

  #endregion

  #region Konstruktory

  public MainWindowViewModel(Zasedani zasedani, App app)
  {
    Zasedani = zasedani;
    App = app;
  }

  #endregion
}