using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public enum RezimBodu
{
  [Description("Informativní")] Informativni,
  [Description("Bere na vědomí")] BereNaVedomi,
  [Description("S usneseními")] SUsnesenimi
}

public class BodProgramu : ObservableObject
{
  public enum TypBoduProgramu
  {
    [Description("Schválení zapisovatele a ověřovatelů")] SchvaleniZapisOver,
    [Description("Schválení programu")] SchvaleniProgramu,
    [Description("Řádný bod")] BodZasedani,
    [Description("Doplněný bod")] DoplnenyBodZasedani,
    [Description("Kontrola minulého zápisu")] KontrolaMinulehoZapisu
  }
  public Guid Id { get; set; } = Guid.NewGuid();
  private string _nadpis = "";
  private string _prubehHtml = "";
  private TypBoduProgramu _typ = TypBoduProgramu.BodZasedani;
  private RezimBodu _rezim;
  public string Nadpis { get => _nadpis; set => SetProperty(ref _nadpis, value ?? ""); }
  public string PrubehHtml { get => _prubehHtml; set => SetProperty(ref _prubehHtml, value ?? ""); }
  public TypBoduProgramu Typ { get => _typ; set => SetProperty(ref _typ, value); }
  public RezimBodu Rezim { get => _rezim; set => SetProperty(ref _rezim, value); }
  public ObservableCollection<Usneseni> Usneseni { get; } = new();
  public ObservableCollection<BodProgramu> Podbody { get; } = new();
  [JsonIgnore] public bool JeBezny => Typ is TypBoduProgramu.BodZasedani or TypBoduProgramu.DoplnenyBodZasedani;

  public BodProgramu Duplikovat(IEnumerable<Zastupitel> zastupitele)
  {
    var kopie = new BodProgramu { Nadpis = Nadpis + " (kopie)", Typ = Typ, PrubehHtml = PrubehHtml, Rezim = Rezim };
    foreach (var u in Usneseni)
    {
      var nove = kopie.PridatUsneseni(zastupitele);
      nove.TextHtml = u.TextHtml;
      foreach (var hlas in nove.VolbyZastupitelu)
        hlas.Volba = u.VolbyZastupitelu.FirstOrDefault(h => h.ZastupitelId == hlas.ZastupitelId)?.Volba ?? HlasovaniZastupitele.VolbaHlasovani.Pro;
    }
    foreach (var podbod in Podbody) kopie.Podbody.Add(podbod.Duplikovat(zastupitele));
    return kopie;
  }

  public void ZmenitRezim(RezimBodu rezim, IEnumerable<Zastupitel> zastupitele)
  {
    if (rezim == RezimBodu.SUsnesenimi && Usneseni.Count == 0) PridatUsneseni(zastupitele);
    else if (rezim != RezimBodu.SUsnesenimi) Usneseni.Clear();
    Rezim = rezim;
  }
  public Usneseni PridatUsneseni(IEnumerable<Zastupitel> zastupitele)
  {
    var usneseni = new Usneseni();
    foreach (var zastupitel in zastupitele)
      usneseni.VolbyZastupitelu.Add(new HlasovaniZastupitele { Zastupitel = zastupitel });
    Usneseni.Add(usneseni);
    Rezim = RezimBodu.SUsnesenimi;
    return usneseni;
  }
}
