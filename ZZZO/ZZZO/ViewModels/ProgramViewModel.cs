using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using ZZZO.Commands;
using ZZZO.Common.API;

namespace ZZZO.ViewModels;

public sealed class RadekBodu : ViewModelBase
{
  public BodProgramu Bod { get; }
  public bool JePodbod { get; set; }
  public string Cislo { get; set; }
  public Thickness Odsazeni => new(JePodbod ? 22 : 0, 0, 0, 0);
  public string Barva => Bod.Typ switch
  {
    BodProgramu.TypBoduProgramu.SchvaleniZapisOver => "#7B61A8",
    BodProgramu.TypBoduProgramu.SchvaleniProgramu => "#4875AD",
    BodProgramu.TypBoduProgramu.KontrolaMinulehoZapisu => "#90703A",
    BodProgramu.TypBoduProgramu.DoplnenyBodZasedani => "#288773",
    _ => "#78838F"
  };
  public string PopisTypu => Converters.EnumConverter.GetEnumDescription(Bod.Typ);
  public RadekBodu(BodProgramu bod) => Bod = bod;
  public void Obnovit() => OnPropertyChanged("");
}

public class ProgramViewModel : ViewModelBase
{
  public ZzzoCore Core { get; }
  public ProgramEntryViewModel EntryViewModel { get; }
  public ObservableCollection<RadekBodu> Radky { get; } = new();
  private BodProgramu _vybranyBod;
  public BodProgramu ChosenProgramEntry
  {
    get => _vybranyBod;
    set { if (SetProperty(ref _vybranyBod, value)) { EntryViewModel.ProgramEntry = value; CommandManager.InvalidateRequerySuggested(); } }
  }
  public ICommand AddProgramEntryCmd { get; }
  public ICommand DuplikovatBodCmd { get; }
  public ICommand PridatPodbodCmd { get; }
  public ICommand ZmenitNaPodbodCmd { get; }
  public ICommand ZmenitNaHlavniCmd { get; }
  public ICommand RemoveProgramEntryCmd { get; }
  public ICommand MoveProgramEntryUpCmd { get; }
  public ICommand MoveProgramEntryDownCmd { get; }

  public ProgramViewModel(ZzzoCore core)
  {
    Core = core;
    EntryViewModel = new(core);
    AddProgramEntryCmd = new RelayCommand(o => PridatBod(o is BodProgramu.TypBoduProgramu t ? t : BodProgramu.TypBoduProgramu.BodZasedani));
    DuplikovatBodCmd = new RelayCommand(async _ =>
    {
      try
      {
        await Controls.TinyMceEditor.DokoncitVseAsync();
        var puvodni = ChosenProgramEntry;
        var kopie = puvodni.Duplikovat(Core.Zasedani.Zastupitele);
        var kolekce = Core.Zasedani.Program.KolekceBodu(puvodni);
        kolekce.Insert(kolekce.IndexOf(puvodni) + 1, kopie);
        ChosenProgramEntry = kopie;
      }
      catch (Exception ex) { MessageBox.Show(ex.Message, "Bod nelze duplikovat", MessageBoxButton.OK, MessageBoxImage.Error); }
    }, _ => ChosenProgramEntry != null);
    PridatPodbodCmd = new RelayCommand(_ => PridatPodbod(), _ => ChosenProgramEntry?.JeBezny == true);
    RemoveProgramEntryCmd = new RelayCommand(_ => OdebratBod(), _ => ChosenProgramEntry != null);
    MoveProgramEntryUpCmd = new RelayCommand(_ => Presunout(-1), _ => LzePresunout(-1));
    MoveProgramEntryDownCmd = new RelayCommand(_ => Presunout(1), _ => LzePresunout(1));
    ZmenitNaPodbodCmd = new RelayCommand(_ => Zanoreni(true), _ => LzeZanorit());
    ZmenitNaHlavniCmd = new RelayCommand(_ => Zanoreni(false), _ => Core.Zasedani?.Program.RodicBodu(ChosenProgramEntry) != null);
    Core.DataZmenena += Obnovit;
    Core.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Core.Zasedani)) { ChosenProgramEntry = null; Obnovit(); } };
    Obnovit();
  }

  private void Obnovit()
  {
    var program = Core.Zasedani?.Program;
    var body = program?.VsechnyBody().ToList() ?? new();
    var cisla = program?.OcislovatBody();
    for (int i = Radky.Count - 1; i >= 0; i--) if (!body.Contains(Radky[i].Bod)) Radky.RemoveAt(i);
    for (int i = 0; i < body.Count; i++)
    {
      var radek = Radky.FirstOrDefault(r => r.Bod == body[i]);
      if (radek == null) { radek = new(body[i]); Radky.Insert(i, radek); }
      else if (Radky.IndexOf(radek) != i) Radky.Move(Radky.IndexOf(radek), i);
      radek.JePodbod = program.RodicBodu(body[i]) != null;
      radek.Cislo = cisla.GetValueOrDefault(body[i].Id, "");
      radek.Obnovit();
    }
    if (!body.Contains(ChosenProgramEntry)) ChosenProgramEntry = body.FirstOrDefault();
    EntryViewModel.Obnovit();
    CommandManager.InvalidateRequerySuggested();
  }

  private void PridatBod(BodProgramu.TypBoduProgramu typ)
  {
    var p = Core.Zasedani.Program;
    var rodic = p.RodicBodu(ChosenProgramEntry) ?? ChosenProgramEntry;
    int index = rodic == null ? p.BodyProgramu.Count : p.BodyProgramu.IndexOf(rodic) + 1;
    var bod = p.VygenerovatBodProgramu(Core.Zasedani, typ);
    p.BodyProgramu.Insert(index, bod);
    ChosenProgramEntry = bod;
  }
  private void PridatPodbod()
  {
    var p = Core.Zasedani.Program;
    var rodic = p.RodicBodu(ChosenProgramEntry) ?? ChosenProgramEntry;
    var bod = p.VygenerovatBodProgramu(Core.Zasedani, BodProgramu.TypBoduProgramu.BodZasedani);
    bod.Nadpis = "Nový podbod";
    rodic.Podbody.Add(bod);
    ChosenProgramEntry = bod;
  }
  private void OdebratBod()
  {
    var bod = ChosenProgramEntry;
    if (MessageBox.Show($"Odstranit bod „{bod.Nadpis}“, jeho {bod.Podbody.Count} podbodů a všechna související usnesení?", "Odstranit bod", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
    var kolekce = Core.Zasedani.Program.KolekceBodu(bod);
    int index = kolekce.IndexOf(bod);
    kolekce.Remove(bod);
    ChosenProgramEntry = kolekce.Count > 0 ? kolekce[Math.Min(index, kolekce.Count - 1)] : Core.Zasedani.Program.BodyProgramu.FirstOrDefault();
  }
  private bool LzePresunout(int smer)
  {
    var kolekce = Core.Zasedani?.Program.KolekceBodu(ChosenProgramEntry);
    int index = kolekce?.IndexOf(ChosenProgramEntry) ?? -1;
    return index >= 0 && index + smer >= 0 && index + smer < kolekce.Count;
  }
  private void Presunout(int smer)
  {
    var bod = ChosenProgramEntry;
    var kolekce = Core.Zasedani.Program.KolekceBodu(bod);
    int index = kolekce.IndexOf(bod);
    kolekce.Move(index, index + smer);
    ChosenProgramEntry = bod;
  }
  private bool LzeZanorit()
  {
    var p = Core.Zasedani?.Program;
    int index = p?.BodyProgramu.IndexOf(ChosenProgramEntry) ?? -1;
    return index > 0 && ChosenProgramEntry.JeBezny && ChosenProgramEntry.Podbody.Count == 0 && p.BodyProgramu[index - 1].JeBezny;
  }
  private void Zanoreni(bool zanorit)
  {
    var p = Core.Zasedani.Program;
    var bod = ChosenProgramEntry;
    if (zanorit)
    {
      var rodic = p.BodyProgramu[p.BodyProgramu.IndexOf(bod) - 1];
      p.BodyProgramu.Remove(bod);
      rodic.Podbody.Add(bod);
    }
    else
    {
      var rodic = p.RodicBodu(bod);
      rodic.Podbody.Remove(bod);
      p.BodyProgramu.Insert(p.BodyProgramu.IndexOf(rodic) + 1, bod);
    }
    ChosenProgramEntry = bod;
  }
}
