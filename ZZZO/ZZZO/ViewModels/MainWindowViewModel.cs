using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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

    NewZasedaniCmd = new RelayCommand(async obj => await ProvestAsync(() => { if (UlozitPredPokracovanim()) Core.NewZaseDani(Zasedani.VytvoritNove()); return Task.CompletedTask; }), obj => !_pracuje);
    LoadZasedaniCmd = new RelayCommand(async obj => await ProvestAsync(() => { if (UlozitPredPokracovanim()) Core.LoadZasedani(); return Task.CompletedTask; }), obj => !_pracuje);
    SaveZasedaniCmd = new RelayCommand(async obj => await ProvestAsync(() => { Core.SaveZasedani(false); return Task.CompletedTask; }), obj => !_pracuje && Core.ZasedaniLoaded);
    SaveZasedaniAsCmd = new RelayCommand(async obj => await ProvestAsync(() => { Core.SaveZasedani(true); return Task.CompletedTask; }), obj => !_pracuje && Core.ZasedaniLoaded);
    AboutAppCmd = new RelayCommand(obj => ShowAboutDialog(), obj => true);
  }

  #endregion

  #region Metody

  private bool _pracuje;
  private bool _zavrit;
  private async Task ProvestAsync(Func<Task> akce)
  {
    if (_pracuje) return;
    _pracuje = true;
    try { await TinyMceEditor.DokoncitVseAsync(); await akce(); }
    catch (Exception ex) { MessageBox.Show(ex.Message, "Operaci nelze dokončit", MessageBoxButton.OK, MessageBoxImage.Error); }
    finally { _pracuje = false; }
  }

  private async void OnAppClosing(object sender, CancelEventArgs e)
  {
    if (_zavrit) return;
    e.Cancel = true;
    await ProvestAsync(() =>
    {
      if (UlozitPredPokracovanim()) { _zavrit = true; ((Window)sender).Close(); }
      return Task.CompletedTask;
    });
  }

  private bool UlozitPredPokracovanim()
  {
    if (!Core.ZasedaniIsDirty) return true;
    var odpoved = MessageBox.Show("Uložit změny před pokračováním?", "Neuložené změny",
      MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
    return odpoved == MessageBoxResult.No || odpoved == MessageBoxResult.Yes && Core.SaveZasedani(false);
  }

  private void ShowAboutDialog()
  {
    DialogHost.Show(new AboutApp());
  }

  #endregion
}
