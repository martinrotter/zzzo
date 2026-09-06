using System.Text.RegularExpressions;
using ZZZO.Common.API;
using static ZZZO.Common.Generators.Generator;
namespace ZZZO.Common.Validace;

public enum OblastValidace { ZakladniUdaje, Zastupitele, Program }
public sealed record ChybaValidace(string Kod, string Zprava, OblastValidace Oblast, Guid? PolozkaId = null, string Vlastnost = null, bool JenZapis = false)
{
  public string Popis => Zprava + (JenZapis ? " (pro zápis)" : "");
  public bool Blokuje(TypDokumentu typ) => !JenZapis || typ == TypDokumentu.Zapis;
}

public static class ValidatorZasedani
{
  public static IReadOnlyList<ChybaValidace> Overit(Zasedani zas)
  {
    var chyby = new List<ChybaValidace>();
    void Chyba(string kod, string zprava, OblastValidace oblast, Guid? id = null, string pole = null, bool zapis = false) =>
      chyby.Add(new(kod, zprava, oblast, id, pole, zapis));
    if (string.IsNullOrWhiteSpace(zas.NazevObce)) Chyba("obec", "Vyplňte název obce.", OblastValidace.ZakladniUdaje, pole: nameof(zas.NazevObce));
    if (zas.Poradi < 1) Chyba("poradi", "Pořadí zasedání musí být kladné.", OblastValidace.ZakladniUdaje, pole: nameof(zas.Poradi));
    if (zas.PocetHostu < 0) Chyba("hoste", "Počet hostů nesmí být záporný.", OblastValidace.ZakladniUdaje, pole: nameof(zas.PocetHostu));
    if (string.IsNullOrWhiteSpace(zas.AdresaKonani.Obec) || string.IsNullOrWhiteSpace(zas.AdresaKonani.PopisMista))
      Chyba("misto", "Vyplňte obec a popis místa konání.", OblastValidace.ZakladniUdaje);
    if (!string.IsNullOrWhiteSpace(zas.AdresaKonani.Psc) && !Regex.IsMatch(zas.AdresaKonani.Psc, @"^\d{3} ?\d{2}$"))
      Chyba("psc", "PSČ musí mít pět číslic.", OblastValidace.ZakladniUdaje);
    var osoby = zas.Zastupitele;
    foreach (var z in osoby)
    {
      if (string.IsNullOrWhiteSpace(z.Jmeno) || string.IsNullOrWhiteSpace(z.Prijmeni))
        Chyba("jmeno", "Vyplňte jméno a příjmení zastupitele.", OblastValidace.Zastupitele, z.Id, zapis: true);
      if (!z.JePritomen && (z.JeRidici || z.JeZapisovatel || z.JeOverovatel))
        Chyba("pritomnost", $"{z.Jmeno} {z.Prijmeni}: nepřítomný nemůže být řídícím, zapisovatelem ani ověřovatelem.", OblastValidace.Zastupitele, z.Id, nameof(z.JePritomen), true);
    }
    void Role(Func<Zastupitel, bool> vyber, string nazev)
    {
      var vybrane = osoby.Where(vyber).ToList();
      if (vybrane.Count != 1) Chyba("role-" + nazev, $"Vyberte právě jednu osobu pro roli: {nazev}.", OblastValidace.Zastupitele, zapis: true);
      if (vybrane.Count > 1) foreach (var z in vybrane) Chyba("duplicita-role", $"Role {nazev} je přiřazena více osobám.", OblastValidace.Zastupitele, z.Id, zapis: true);
    }
    Role(z => z.JeRidici, "řídící"); Role(z => z.JeZapisovatel, "zapisovatel"); Role(z => z.JeStarosta, "starosta");
    if (osoby.Count(z => z.JeOverovatel) < 2) Chyba("overovatele", "Vyberte alespoň dva přítomné ověřovatele.", OblastValidace.Zastupitele, zapis: true);

    var body = zas.Program.VsechnyBody().ToList();
    foreach (var typ in new[] { BodProgramu.TypBoduProgramu.SchvaleniProgramu, BodProgramu.TypBoduProgramu.SchvaleniZapisOver, BodProgramu.TypBoduProgramu.KontrolaMinulehoZapisu })
    {
      var vybrane = body.Where(b => b.Typ == typ).ToList();
      string nazev = typ switch { BodProgramu.TypBoduProgramu.SchvaleniProgramu => "Schválení programu", BodProgramu.TypBoduProgramu.SchvaleniZapisOver => "Schválení zapisovatele a ověřovatelů", _ => "Kontrola minulého zápisu" };
      if (vybrane.Count == 0) Chyba("chybi-" + typ, $"V programu chybí bod „{nazev}“.", OblastValidace.Program, zapis: true);
      if (vybrane.Count > 1) foreach (var b in vybrane) Chyba("duplicita-bodu", $"Bod „{nazev}“ má být v programu právě jednou.", OblastValidace.Program, b.Id, zapis: true);
    }
    foreach (var b in body)
    {
      if (string.IsNullOrWhiteSpace(b.Nadpis)) Chyba("nadpis", "Bod programu nemá název.", OblastValidace.Program, b.Id, nameof(b.Nadpis));
      if (!Enum.IsDefined(b.Typ) || !Enum.IsDefined(b.Rezim)) Chyba("typ", "Bod má neznámý typ nebo režim.", OblastValidace.Program, b.Id);
      if (b.Rezim == RezimBodu.SUsnesenimi && b.Usneseni.Count == 0)
        Chyba("prazdna-usneseni", $"„{b.Nadpis}“: přidejte usnesení nebo změňte režim bodu.", OblastValidace.Program, b.Id, zapis: true);
      if (b.Rezim != RezimBodu.SUsnesenimi && b.Usneseni.Count != 0)
        Chyba("rozpor-rezimu", $"„{b.Nadpis}“: tento režim nemůže obsahovat usnesení.", OblastValidace.Program, b.Id, zapis: true);
      if (b.Typ is BodProgramu.TypBoduProgramu.SchvaleniProgramu or BodProgramu.TypBoduProgramu.SchvaleniZapisOver && b.Rezim != RezimBodu.SUsnesenimi)
        Chyba("schvaleni", $"„{b.Nadpis}“: schvalovací bod musí mít usnesení s hlasováním.", OblastValidace.Program, b.Id, zapis: true);
      var rodic = zas.Program.RodicBodu(b);
      if (rodic != null && (!b.JeBezny || !rodic.JeBezny || b.Podbody.Count > 0))
        Chyba("podbody", "Podbod může být pouze řádný nebo doplněný, pod běžným hlavním bodem a bez dalšího zanoření.", OblastValidace.Program, b.Id);
      foreach (var u in b.Usneseni)
      {
        if (string.IsNullOrWhiteSpace(HtmlObsah.ProstyText(u.TextHtml)))
          Chyba("text-usneseni", $"„{b.Nadpis}“, usnesení {b.Usneseni.IndexOf(u) + 1}: vyplňte text.", OblastValidace.Program, u.Id, nameof(u.TextHtml), true);
        if (!VyhodnoceniHlasovani.Vyhodnotit(zas, u).JePlatne)
          Chyba("hlasy", $"„{b.Nadpis}“, usnesení {b.Usneseni.IndexOf(u) + 1}: chybějící, duplicitní nebo neplatné hlasy.", OblastValidace.Program, u.Id, zapis: true);
      }
    }
    var ids = osoby.Select(z => z.Id).Concat(body.Select(b => b.Id)).Concat(body.SelectMany(b => b.Usneseni).Select(u => u.Id)).ToList();
    if (ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Count)
      Chyba("identifikatory", "Dokument obsahuje chybějící nebo duplicitní identifikátory položek.", OblastValidace.Program);
    return chyby;
  }
}
