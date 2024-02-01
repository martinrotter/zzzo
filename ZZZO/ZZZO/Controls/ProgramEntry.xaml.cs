using System.Windows;
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

    #region Metody

    private void AddUsneseni(object sender, RoutedEventArgs e)
    {
      App.Current.Zasedani.AddUsneseni(BodProgramu, new Usneseni
      {
        Text = "Text usnesení"
      });
    }

    private void RemoveUsneseni(object sender, RoutedEventArgs e)
    {
      if (CmbUsneseni.SelectedValue is Usneseni usneseni)
      {
        BodProgramu.Usneseni.Remove(usneseni);
      }
    }

    #endregion
  }
}