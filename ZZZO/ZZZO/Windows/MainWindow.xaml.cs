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
      App.Current.Core.PrejitNaChybu += chyba =>
      {
        if (chyba.Oblast == Common.Validace.OblastValidace.Program)
        {
          TcZasedani.SelectedIndex = 1;
          var vm = (ViewModels.ProgramViewModel)UcProgram.DataContext;
          var bod = vm.Core.Zasedani.Program.VsechnyBody().FirstOrDefault(b => b.Id == chyba.PolozkaId || b.Usneseni.Any(u => u.Id == chyba.PolozkaId));
          if (bod != null)
          {
            vm.ChosenProgramEntry = bod;
            var usneseni = bod.Usneseni.FirstOrDefault(u => u.Id == chyba.PolozkaId);
            if (usneseni != null) { vm.EntryViewModel.ChosenUsneseni = usneseni; vm.EntryViewModel.VybranaZalozka = 1; }
            else vm.EntryViewModel.VybranaZalozka = 0;
            UcProgram.LvProgram.ScrollIntoView(vm.Radky.FirstOrDefault(r => r.Bod == bod));
            if (usneseni == null) UcProgram.UcProgramEntry.NazevBodu.Focus();
          }
        }
        else
        {
          TcZasedani.SelectedIndex = 0;
          var vm = (ViewModels.BasicInfoViewModel)UcBasicInfo.DataContext;
          if (chyba.Oblast == Common.Validace.OblastValidace.Zastupitele)
          {
            vm.ChosenZastupitel = vm.Zastupitele.FirstOrDefault(z => z.Id == chyba.PolozkaId) ?? vm.Zastupitele.FirstOrDefault();
            if (vm.ChosenZastupitel != null) UcBasicInfo.LvZastupitele.ScrollIntoView(vm.ChosenZastupitel);
            UcBasicInfo.LvZastupitele.Focus();
          }
          else
          {
            var cil = chyba.Vlastnost switch
            {
              nameof(Common.API.Zasedani.Poradi) => UcBasicInfo.TbPoradi,
              nameof(Common.API.Zasedani.PocetHostu) => UcBasicInfo.TbHoste,
              _ => UcBasicInfo.TbObec
            };
            cil.BringIntoView(); cil.Focus();
          }
        }
      };

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
