using System.Windows.Controls;
using ZZZO.Common.API;

namespace ZZZO.Controls
{
  /// <summary>
  /// Interaction logic for ProgramEntry.xaml
  /// </summary>
  public partial class ProgramEntry : UserControl
  {
    #region Vlastnosti

    private BodProgramu BodProgramu
    {
      get => DataContext as BodProgramu;
    }

    #endregion

    #region Konstruktory

    public ProgramEntry()
    {
      InitializeComponent();
    }

    #endregion
  }
}