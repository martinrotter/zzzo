using System.Windows;
using System.Windows.Controls;
using ZZZO.Common.API;

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
    }

    #endregion

    #region Metody

    private void AddProgramEntry(object sender, RoutedEventArgs e)
    {
      App.Current.Core.Zasedani.Program.BodyProgramu.Add(new BodProgramu
      {
        Nadpis = "Nový bod programu",
        Text = "Text bodu programu"
      });
    }

    private void MoveProgramEntryDown(object sender, RoutedEventArgs e)
    {
      if (LvProgram.SelectedValue is BodProgramu bod)
      {
        int idx = App.Current.Core.Zasedani.Program.BodyProgramu.IndexOf(bod);

        if (idx < App.Current.Core.Zasedani.Program.BodyProgramu.Count - 1)
        {
          App.Current.Core.Zasedani.Program.BodyProgramu.Move(idx, idx + 1);
        }
      }
    }

    private void MoveProgramEntryUp(object sender, RoutedEventArgs e)
    {
      if (LvProgram.SelectedValue is BodProgramu bod)
      {
        int idx = App.Current.Core.Zasedani.Program.BodyProgramu.IndexOf(bod);

        if (idx > 0)
        {
          App.Current.Core.Zasedani.Program.BodyProgramu.Move(idx, idx - 1);
        }
      }
    }

    private void RemoveProgramEntry(object sender, RoutedEventArgs e)
    {
      if (LvProgram.SelectedValue is BodProgramu bod)
      {
        App.Current.Core.Zasedani.Program.BodyProgramu.Remove(bod);
      }
    }

    #endregion
  }
}