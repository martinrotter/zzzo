using System.ComponentModel;
using System.Windows;

namespace ZZZO.Windows
{
  public partial class MainWindow : Window
  {
    #region Konstruktory

    public MainWindow()
    {
      InitializeComponent();
      App.Current.SetDataContexts(this);
    }

    #endregion

    #region Metody

    private void QuitApp(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void ShowAboutAppDialog(object sender, RoutedEventArgs e)
    {
      AboutWindow d = new AboutWindow
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