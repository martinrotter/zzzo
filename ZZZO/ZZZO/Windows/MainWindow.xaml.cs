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
    private readonly NastaveniAplikace _nastaveni;

    #region Konstruktory

    public MainWindow()
    {
      InitializeComponent();
      _nastaveni = NastaveniAplikace.Nacist(App.Current.SouborNastaveni);
      App.Current.SetDataContexts(this);
      Loaded += (_, _) => ObnovitRozlozeni();
      Closed += (_, _) => UlozitRozlozeni();
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

    private void ObnovitRozlozeni()
    {
      if (JeRozmer(_nastaveni.SirkaOkna, MinWidth) && JeRozmer(_nastaveni.VyskaOkna, MinHeight))
      {
        Width = Math.Min(_nastaveni.SirkaOkna!.Value, Math.Max(MinWidth, SystemParameters.VirtualScreenWidth));
        Height = Math.Min(_nastaveni.VyskaOkna!.Value, Math.Max(MinHeight, SystemParameters.VirtualScreenHeight));
      }

      if (_nastaveni.OknoVlevo is double vlevo && _nastaveni.OknoNahore is double nahore &&
          double.IsFinite(vlevo) && double.IsFinite(nahore))
      {
        const double viditelnyOkraj = 80;
        Left = Math.Clamp(vlevo, SystemParameters.VirtualScreenLeft - Width + viditelnyOkraj,
          SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - viditelnyOkraj);
        Top = Math.Clamp(nahore, SystemParameters.VirtualScreenTop,
          SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - viditelnyOkraj);
        WindowStartupLocation = WindowStartupLocation.Manual;
      }

      var bod = UcProgram.UcProgramEntry;
      if (JeRozmer(_nastaveni.VyskaSeznamuUsneseni, 52))
        bod.RadekSeznamuUsneseni.Height = new GridLength(_nastaveni.VyskaSeznamuUsneseni!.Value);
      if (JeRozmer(_nastaveni.SirkaPaneluHlasovani, 320))
        bod.DetailUsneseni.SloupecHlasovani.Width = new GridLength(_nastaveni.SirkaPaneluHlasovani!.Value);

      if (_nastaveni.OknoMaximalizovane) WindowState = WindowState.Maximized;
    }

    private void UlozitRozlozeni()
    {
      Rect rozmery = RestoreBounds;
      if (rozmery.Width < MinWidth || rozmery.Height < MinHeight)
        rozmery = new Rect(Left, Top, ActualWidth, ActualHeight);

      NastaveniAplikace.Ulozit(App.Current.SouborNastaveni, new NastaveniAplikace
      {
        OknoVlevo = rozmery.Left,
        OknoNahore = rozmery.Top,
        SirkaOkna = rozmery.Width,
        VyskaOkna = rozmery.Height,
        OknoMaximalizovane = WindowState == WindowState.Maximized,
        VyskaSeznamuUsneseni = UcProgram.UcProgramEntry.RadekSeznamuUsneseni.Height.Value,
        SirkaPaneluHlasovani = UcProgram.UcProgramEntry.DetailUsneseni.SloupecHlasovani.Width.Value
      });
    }

    private static bool JeRozmer(double? hodnota, double minimum) =>
      hodnota is double rozmer && double.IsFinite(rozmer) && rozmer >= minimum && rozmer <= 10000;

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
