using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using ZZZO.Commands;
using ZZZO.Common.API;
using ZZZO.Controls;
using ZZZO.Windows;
using Constants = ZZZO.Common.Constants;

namespace ZZZO.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  #region Vlastnosti

  public ICommand AboutAppCmd
  {
    get;
  }

  public App App
  {
    get;
  }

  public ZzzoCore Core
  {
    get;
  }

  public ICommand LoadZasedaniCmd
  {
    get;
  }

  public ICommand NewZasedaniCmd
  {
    get;
  }

  public ICommand SaveZasedaniAsCmd
  {
    get;
  }

  public ICommand SaveZasedaniCmd
  {
    get;
  }

  public string WindowTitle
  {
    get
    {
      if (Core.Zasedani == null)
      {
        return Constants.Names.AppLongName;
      }
      else
      {
        if (Core.Zasedani.VystupniSoubor != null)
        {
          return $"{Constants.Names.AppLongName} - {Core.Zasedani.VystupniSoubor}";
        }
        else
        {
          return $"{Constants.Names.AppLongName} - neuložený zápis";
        }
      }
    }
  }

  #endregion

  #region Konstruktory

  public MainWindowViewModel()
  {
  }

  public MainWindowViewModel(MainWindow window, App app, ZzzoCore core)
  {
    // View is only used to bind necessary events.
    window.Closing += OnAppClosing;

    App = app;
    Core = core;

    Core.PropertyChanged += (sender, args) =>
    {
      if (args.PropertyName == nameof(ZzzoCore.Zasedani))
      {
        OnPropertyChanged(nameof(WindowTitle));

        if (Core.Zasedani != null)
        {
          Core.Zasedani.PropertyChanged += (o, zasArgs) =>
          {
            if (zasArgs.PropertyName == nameof(Zasedani.VystupniSoubor))
            {
              OnPropertyChanged(nameof(WindowTitle));
            }
          };
        }
      }
    };

    NewZasedaniCmd = new RelayCommand(obj => NewZasedani(), obj => true);
    LoadZasedaniCmd = new RelayCommand(obj => LoadZasedani(), obj => true);
    SaveZasedaniCmd = new RelayCommand(obj => SaveZasedani(), obj => true);
    SaveZasedaniAsCmd = new RelayCommand(obj => SaveZasedaniAs(), obj => true);
    AboutAppCmd = new RelayCommand(obj => ShowAboutDialog(), obj => true);
  }

  #endregion

  #region Metody

  private void LoadZasedani()
  {
    if (!SaveIfDirtyAndContinue())
    {
      return;
    }

    try
    {
      Core.LoadZasedani();
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Chyba při načítání zápisu: {ex.Message}.", "Nelze načíst zápis", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }

  private void NewZasedani()
  {
    if (!SaveIfDirtyAndContinue())
    {
      return;
    }

    Zasedani zas = Zasedani.GenerateSample();

    Core.NewZaseDani(zas);
  }

  private void OnAppClosing(object sender, CancelEventArgs e)
  {
    if (!SaveIfDirtyAndContinue())
    {
      e.Cancel = true;
    }
  }

  private bool SaveIfDirtyAndContinue()
  {
    if (Core.ZasedaniIsDirty)
    {
      if (MessageBox.Show(
            "Máte nějaké neuložené změny, chcete nejdříve uložit vaši současnou práci?",
            "Neuložené změny",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.Yes) == MessageBoxResult.Yes)
      {
        if (!SaveZasedani())
        {
          // User did not save his unsaved work, abort.
          return false;
        }
      }
      else
      {
        // User does not need to save changes.
      }
    }

    return true;
  }

  private bool SaveZasedani()
  {
    try
    {
      return Core.SaveZasedani(false);
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Chyba při ukládání zápisu: {ex.Message}.", "Nelze uložit zápis", MessageBoxButton.OK, MessageBoxImage.Error);
      return false;
    }
  }

  private bool SaveZasedaniAs()
  {
    try
    {
      return Core.SaveZasedani(true);
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Chyba při ukládání zápisu: {ex.Message}.", "Nelze uložit zápis", MessageBoxButton.OK, MessageBoxImage.Error);
      return false;
    }
  }

  private void ShowAboutDialog()
  {
    DialogHost.Show(new AboutApp());
  }

  #endregion
}