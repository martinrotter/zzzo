using System.Windows;
using System.Windows.Input;

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

    private void OnDialogHostKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogHost.IsOpen = false;
      }
    }

    #endregion
  }
}