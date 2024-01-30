using System.ComponentModel;
using System.Windows;
using ZZZO.Common;
using ZZZO.Common.API;

namespace ZZZO
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
        JeStarosta = false,
        JePritomen = true
      });

      App.Current.ResetZasedani(zas, this);

      App.Current.Zasedani.InitializeTree();
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
      MessageBox.Show($"{Constants.Names.AppShortName} - {Constants.Names.AppLongName}\n\n" +
                      $"Verze: {Constants.Names.AppVersion}\n\n\n" +
                      $"Některé ikony poskytl: {Constants.Names.Freepik}", "O aplikaci",
        MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion
  }
}