using System.Windows.Controls;
using System.Windows;
namespace ZZZO.Controls;
public partial class Program : UserControl
{
  public Program() { InitializeComponent(); }
  private void OtevritNabidku(object sender, RoutedEventArgs e)
  {
    var tlacitko = (Button)sender;
    tlacitko.ContextMenu.DataContext = DataContext;
    tlacitko.ContextMenu.PlacementTarget = tlacitko;
    tlacitko.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
    tlacitko.ContextMenu.IsOpen = true;
  }
}
