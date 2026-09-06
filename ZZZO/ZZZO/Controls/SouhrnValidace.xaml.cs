using System.Windows.Controls;
namespace ZZZO.Controls;
public partial class SouhrnValidace : UserControl
{
  private void PrejitNaProblem(object sender, System.Windows.RoutedEventArgs e) => Dispatcher.BeginInvoke(new System.Action(() => TlacitkoValidace.IsChecked = false));
  public SouhrnValidace() => InitializeComponent();
}
