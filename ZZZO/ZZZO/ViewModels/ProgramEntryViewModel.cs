using System.Windows.Input;
using System.Windows;
using ZZZO.Commands;
using ZZZO.Common.API;
namespace ZZZO.ViewModels;

public class ProgramEntryViewModel : ViewModelBase
{
  public ZzzoCore Core { get; }
  public ResolutionViewModel ResolutionViewModel { get; }
  private BodProgramu _bod;
  private Usneseni _usneseni;
  private int _zalozka;
  public int VybranaZalozka { get => _zalozka; set => SetProperty(ref _zalozka, value); }
  public BodProgramu ProgramEntry
  {
    get => _bod;
    set
    {
      if (!SetProperty(ref _bod, value)) return;
      ChosenUsneseni = value?.Usneseni.FirstOrDefault();
      VybranaZalozka = 0;
      Obnovit();
    }
  }
  public Usneseni ChosenUsneseni
  {
    get => _usneseni;
    set { if (SetProperty(ref _usneseni, value)) ResolutionViewModel.Usneseni = value; }
  }
  public IEnumerable<BodProgramu.TypBoduProgramu> TypyBoduProgramu => Enum.GetValues<BodProgramu.TypBoduProgramu>();
  public IEnumerable<RezimBodu> Rezimy => Enum.GetValues<RezimBodu>();
  public bool MaUsneseni => ProgramEntry?.Rezim == RezimBodu.SUsnesenimi;
  public string NadpisUsneseni => $"Usnesení ({ProgramEntry?.Usneseni.Count ?? 0})";
  public RezimBodu Rezim
  {
    get => ProgramEntry?.Rezim ?? RezimBodu.Informativni;
    set
    {
      if (ProgramEntry == null || value == Rezim) return;
      if (value != RezimBodu.SUsnesenimi && ProgramEntry.Usneseni.Count > 0 &&
        MessageBox.Show($"Změna režimu odstraní {ProgramEntry.Usneseni.Count} usnesení včetně hlasování. Pokračovat?", "Změna režimu bodu", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
      { OnPropertyChanged(nameof(Rezim)); return; }
      ProgramEntry.ZmenitRezim(value, Core.Zasedani.Zastupitele);
      ChosenUsneseni = ProgramEntry.Usneseni.FirstOrDefault();
      VybranaZalozka = MaUsneseni ? 1 : 0;
      Obnovit();
    }
  }
  public ICommand AddUsneseniCmd { get; }
  public ICommand RemoveUsneseniCmd { get; }
  public ICommand UsneseniNahoruCmd { get; }
  public ICommand UsneseniDoluCmd { get; }
  public ProgramEntryViewModel(ZzzoCore core)
  {
    Core = core;
    ResolutionViewModel = new(core);
    AddUsneseniCmd = new RelayCommand(_ => { ChosenUsneseni = ProgramEntry.PridatUsneseni(Core.Zasedani.Zastupitele); VybranaZalozka = 1; }, _ => ProgramEntry != null);
    RemoveUsneseniCmd = new RelayCommand(_ => Odebrat(), _ => ChosenUsneseni != null);
    UsneseniNahoruCmd = new RelayCommand(_ => Presunout(-1), _ => LzePresunout(-1));
    UsneseniDoluCmd = new RelayCommand(_ => Presunout(1), _ => LzePresunout(1));
  }
  public void Obnovit()
  {
    if (ProgramEntry != null && !ProgramEntry.Usneseni.Contains(ChosenUsneseni)) ChosenUsneseni = ProgramEntry.Usneseni.FirstOrDefault();
    if (!MaUsneseni) VybranaZalozka = 0;
    OnPropertyChanged(nameof(Rezim)); OnPropertyChanged(nameof(MaUsneseni)); OnPropertyChanged(nameof(NadpisUsneseni));
    ResolutionViewModel.Obnovit();
  }
  private bool LzePresunout(int smer)
  {
    int i = ProgramEntry?.Usneseni.IndexOf(ChosenUsneseni) ?? -1;
    return i >= 0 && i + smer >= 0 && i + smer < ProgramEntry.Usneseni.Count;
  }
  private void Presunout(int smer) { int i = ProgramEntry.Usneseni.IndexOf(ChosenUsneseni); ProgramEntry.Usneseni.Move(i, i + smer); }
  private void Odebrat()
  {
    var u = ChosenUsneseni;
    if (MessageBox.Show("Odstranit vybrané usnesení včetně hlasování?", "Odstranit usnesení", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
    int i = ProgramEntry.Usneseni.IndexOf(u);
    ProgramEntry.Usneseni.Remove(u);
    ChosenUsneseni = ProgramEntry.Usneseni.Count == 0 ? null : ProgramEntry.Usneseni[Math.Min(i, ProgramEntry.Usneseni.Count - 1)];
  }
}
