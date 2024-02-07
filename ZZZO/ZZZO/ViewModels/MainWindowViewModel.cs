using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.API;
using ZZZO.Windows;

namespace ZZZO.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  #region Vlastnosti

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

  public ICommand SaveZasedaniCmd
  {
    get;
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

    NewZasedaniCmd = new RelayCommand(obj => NewZasedani(), obj => true);
    LoadZasedaniCmd = new RelayCommand(obj => LoadZasedani(), obj => true);
    SaveZasedaniCmd = new RelayCommand(obj => SaveZasedani(), obj => true);
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

    Zasedani zas = new Zasedani();

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "csacsa",
      Jmeno = "csacas",
      JeOverovatel = false,
      JeStarosta = true,
      JeZapisovatel = true,
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "csacsa",
      Jmeno = "csacas",
      JeOverovatel = true,
      JeStarosta = false,
      JeRidici = true,
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "csacsa",
      Jmeno = "csacas",
      JeOverovatel = true,
      JeStarosta = false,
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "csacsa",
      Jmeno = "csacas",
      JeOverovatel = true,
      JeStarosta = false,
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "csacsa",
      Jmeno = "csacas",
      JeOverovatel = true,
      JeStarosta = false,
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "csacsa",
      Jmeno = "csacas",
      JeOverovatel = false,
      JeStarosta = false,
      JePritomen = false
    });

    zas.AdresaKonani = new Adresa
    {
      CisloPopisneOrientacni = "12",
      Obec = "Krakatit",
      Psc = "779 00",
      Ulice = "Ulicce",
      PopisMista = "zasedací místnost"
    };

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      SchvalovaniProgramu = true,
      Nadpis = "Schvalování programu"
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "První"
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Druhy",
      JePodbod = true
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Treti",
      JePodbod = true
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Ctvrty"
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Paty",
      JeDoplneny = true,
      JePodbod = true
    });

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
      return Core.SaveZasedani();
    }
    catch (Exception ex)
    {
      MessageBox.Show($"Chyba při ukládání zápisu: {ex.Message}.", "Nelze uložit zápis", MessageBoxButton.OK, MessageBoxImage.Error);
      return false;
    }
  }

  #endregion
}