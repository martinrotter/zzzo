using System.ComponentModel;
using System.Windows;
using MaterialDesignThemes.Wpf;

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

  }
}