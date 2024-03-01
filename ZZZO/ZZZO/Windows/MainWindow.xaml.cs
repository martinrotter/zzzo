using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors.Core;
using ZZZO.Common;

namespace ZZZO.Windows
{
  public partial class MainWindow : Window
  {
    #region Konstruktory

    public MainWindow()
    {
      InitializeComponent();
      App.Current.SetDataContexts(this);

      Utils.HookKeyShortctut(
        this,
        DataContext,
        new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl+N"),
        MenuNewZasedani);
      Utils.HookKeyShortctut(
        this,
        DataContext,
        new KeyGesture(Key.L, ModifierKeys.Control, "Ctrl+L"),
        MenuLoadZasedani);
      Utils.HookKeyShortctut(
        this,
        DataContext,
        new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl+S"),
        MenuSaveZasedani);
      Utils.HookKeyShortctut(
        this,
        DataContext,
        new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+S"),
        MenuSaveZasedaniAs);
      Utils.HookKeyShortctut(
        this,
        DataContext,
        new KeyGesture(Key.Q, ModifierKeys.Control, "Ctrl+Q"),
        MenuQuitApp);
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