using System.Windows;
using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.API;

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

  #endregion

  public ICommand NewZasedaniCmd
  {
    get;
  }

  public ICommand LoadZasedaniCmd
  {
    get;
  }

  public ICommand SaveZasedaniCmd
  {
    get;
  }

  #region Konstruktory

  public MainWindowViewModel()
  {
  }

  public MainWindowViewModel(App app, ZzzoCore core)
  {
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
    Core.LoadZasedani();
  }

  private void NewZasedani()
  {
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

    Core.Zasedani = zas;
  }

  private void SaveZasedani()
  {
    Core.SaveZasedani();
  }

  #endregion
}