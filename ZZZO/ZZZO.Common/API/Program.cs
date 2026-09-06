using System.Collections.ObjectModel;
namespace ZZZO.Common.API;

public class Program : ObservableObject
{
  public ObservableCollection<BodProgramu> BodyProgramu { get; } = new();
  public IEnumerable<BodProgramu> VsechnyBody()
  {
    foreach (var bod in BodyProgramu)
    {
      yield return bod;
      foreach (var podbod in ProjitPodbody(bod)) yield return podbod;
    }
  }
  private static IEnumerable<BodProgramu> ProjitPodbody(BodProgramu bod)
  {
    foreach (var podbod in bod.Podbody)
    {
      yield return podbod;
      foreach (var dalsi in ProjitPodbody(podbod)) yield return dalsi;
    }
  }
  public ObservableCollection<BodProgramu> KolekceBodu(BodProgramu bod) =>
    BodyProgramu.Contains(bod) ? BodyProgramu : VsechnyBody().FirstOrDefault(b => b.Podbody.Contains(bod))?.Podbody;
  public BodProgramu RodicBodu(BodProgramu bod) => VsechnyBody().FirstOrDefault(b => b.Podbody.Contains(bod));
  public Dictionary<Guid, string> OcislovatBody()
  {
    var vysledek = new Dictionary<Guid, string>();
    int poradi = 0;
    foreach (var bod in BodyProgramu)
    {
      vysledek[bod.Id] = bod.JeBezny ? $"{++poradi}. " : "";
      for (int i = 0; i < bod.Podbody.Count; i++)
        vysledek[bod.Podbody[i].Id] = $"{poradi}{Pismeno(i)}. ";
    }
    return vysledek;
  }
  private static string Pismeno(int index)
  {
    string text = "";
    do { text = (char)('a' + index % 26) + text; index = index / 26 - 1; } while (index >= 0);
    return text;
  }
  public BodProgramu VygenerovatBodProgramu(Zasedani zas, BodProgramu.TypBoduProgramu typProgramu, bool bezUsneseni = false)
  {
    var bod = new BodProgramu { Typ = typProgramu };
    bod.Nadpis = typProgramu switch
    {
      BodProgramu.TypBoduProgramu.SchvaleniZapisOver => "Schválení zapisovatele a ověřovatelů zápisu",
      BodProgramu.TypBoduProgramu.SchvaleniProgramu => "Schválení programu",
      BodProgramu.TypBoduProgramu.KontrolaMinulehoZapisu => "Kontrola zápisu a plnění usnesení z minulého zasedání ZO",
      BodProgramu.TypBoduProgramu.DoplnenyBodZasedani => "Nový doplněný bod",
      _ => "Nový bod"
    };
    if (typProgramu == BodProgramu.TypBoduProgramu.KontrolaMinulehoZapisu)
      bod.PrubehHtml = "<p>Starosta obce zhodnotil program z minulého jednání ZO a informoval přítomné zastupitele i veřejnost o projednaných bodech a splněných úkolech.</p>";
    if (!bezUsneseni && typProgramu is BodProgramu.TypBoduProgramu.SchvaleniZapisOver or BodProgramu.TypBoduProgramu.SchvaleniProgramu)
      bod.PridatUsneseni(zas.Zastupitele).TextHtml = $"<p>{HtmlObsah.Zakodovat(bod.Nadpis)}</p>";
    return bod;
  }
}
