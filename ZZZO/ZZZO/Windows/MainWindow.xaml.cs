using System.ComponentModel;
using System.Windows;
using ZZZO.Common;
using ZZZO.Common.API;

namespace ZZZO.Windows
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Konstruktory

    public MainWindow()
    {
      InitializeComponent();
      App.Current.ResetZasedani(null, this);
    }

    #endregion

    #region Metody

    protected override void OnClosing(CancelEventArgs e)
    {
      base.OnClosing(e);

      if ((App.Current.Zasedani?.Spinave).GetValueOrDefault())
      {
        MessageBox.Show("Neuložené zasedání.", "Neuložené zasedání", MessageBoxButton.OK);
        e.Cancel = true;
      }
    }

    private void LoadZasedani(object sender, RoutedEventArgs e)
    {
      App.Current.LoadZasedani(this);
    }

    private void NewZasedani(object sender, RoutedEventArgs e)
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

      App.Current.ResetZasedani(zas, this);
    }

    private void QuitApp(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void SaveZasedani(object sender, RoutedEventArgs e)
    {
      App.Current.SaveZasedani();
    }

    private void ShowAboutAppDialog(object sender, RoutedEventArgs e)
    {
      var d = new AboutWindow
      {
        Owner = this
      };

      d.ShowDialog();

      /*
      MessageBox.Show($"{Constants.Names.AppShortName} - {Constants.Names.AppLongName}\n\n" +
                      $"Verze: {Constants.Names.AppVersion}\n\n\n" +
                      $"Některé ikony poskytl: {Constants.Names.Freepik}", "O aplikaci",
        MessageBoxButton.OK, MessageBoxImage.Information);
      */
    }

    #endregion
  }
}