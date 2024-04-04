using System.Windows.Controls;
using System.Windows.Input;

namespace ZZZO.Controls
{
  public partial class Generator : UserControl
  {
    #region Konstruktory

    public Generator()
    {
      InitializeComponent();

      WebBrowser.PreviewMouseWheel += BrowserPreviewMouseWheel;
      WebBrowser.KeyUp += BrowserKeyUp;
    }

    #endregion

    #region Metody

    private void BrowserKeyUp(object sender, KeyEventArgs e)
    {
      if (Keyboard.Modifiers != ModifierKeys.Control)
      {
        return;
      }

      if (e.Key == Key.Add)
      {
        WebBrowser.ZoomInCommand.Execute(null);
      }

      if (e.Key == Key.Subtract)
      {
        WebBrowser.ZoomOutCommand.Execute(null);
      }

      if (e.Key == Key.NumPad0)
      {
        WebBrowser.ZoomLevel = 0;
      }
    }

    private void BrowserPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (Keyboard.Modifiers != ModifierKeys.Control)
      {
        return;
      }

      if (e.Delta > 0)
      {
        WebBrowser.ZoomInCommand.Execute(null);
      }
      else
      {
        WebBrowser.ZoomOutCommand.Execute(null);
      }

      e.Handled = true;
    }

    #endregion
  }
}