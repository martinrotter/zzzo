using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ZZZO.Common;
using ZZZO.Common.API;

namespace ZZZO
{
  /// <summary>
  ///   Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
      App.Current.ResetZasedani(null, this);
    }

    private void ShowAboutAppDialog(object sender, RoutedEventArgs e)
    {
      MessageBox.Show($"{Constants.Names.AppShortName} - {Constants.Names.AppLongName}\n\n" +
                      $"Verze: {Constants.Names.AppVersion}\n\n\n" +
                      $"Některé ikony poskytl: {Constants.Names.Freepik}", "O aplikaci",
        MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void QuitApp(object sender, RoutedEventArgs e)
    {
      Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      base.OnClosing(e);

      if ((App.Current.Zasedani?.Spinave).GetValueOrDefault())
      {
        MessageBox.Show("Neuložené zasedání.", "Neuložené zasedání", MessageBoxButton.OK);
        e.Cancel = true;
      }
    }

    private void NewZasedani(object sender, RoutedEventArgs e)
    {
      App.Current.ResetZasedani(new Zasedani()
      {
        NazevObce = "Svésedlice",
        Zastupitele = new ObservableCollection<Zastupitel>
        {
          new Zastupitel
          {
            JeOverovatel = true,
            Jmeno = "Pepa",
            Prijmeni = "Vyskoč"
          },
          new Zastupitel
          {
            JeOverovatel = false,
            Jmeno = "Pep 2a",
            Prijmeni = "Vyskoč 2"
          },
          new Zastupitel
          {
            JeOverovatel = false,
            Jmeno = "Pep 2a",
            Prijmeni = "Vyskoč 2"
          },
          new Zastupitel
          {
            JeOverovatel = false,
            Jmeno = "Pep 2a",
            Prijmeni = "Vyskoč 2"
          },
          new Zastupitel
          {
            JeOverovatel = false,
            Jmeno = "Pep 2a",
            Prijmeni = "Vyskoč 2"
          },
          new Zastupitel
          {
            JeOverovatel = false,
            Jmeno = "Pep 2a",
            Prijmeni = "Vyskoč 2"
          },
          new Zastupitel
          {
            JeOverovatel = false,
            Jmeno = "Pep 2a",
            Prijmeni = "Vyskoč 2"
          }
        }
      }, this);
    }

    private void SaveZasedani(object sender, RoutedEventArgs e)
    {
      App.Current.SaveZasedani();
    }
  }
}