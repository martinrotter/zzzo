using System.Windows.Controls;
using ZZZO.ViewModels;

namespace ZZZO.Controls
{
  /// <summary>
  /// Interaction logic for Program.xaml
  /// </summary>
  public partial class Program : UserControl
  {
    #region Konstruktory

    public Program()
    {
      InitializeComponent();

      LvProgram.SelectionChanged += LvProgram_SelectionChanged;
    }

    private void LvProgram_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //Lv
    }

    #endregion
  }
}