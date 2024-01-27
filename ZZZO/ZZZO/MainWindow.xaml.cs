using System.ComponentModel;
using System.Data;
using System.Windows;
using ZZZO.Common;

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
    }
  }
}