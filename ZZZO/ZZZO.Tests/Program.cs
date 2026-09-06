using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;
using HtmlAgilityPack;
using ZZZO.Common;
using ZZZO.Common.API;
using ZZZO.Common.Generators;
using ZZZO.Common.Validace;
using static ZZZO.Common.Generators.Generator;
using static ZZZO.Common.API.BodProgramu;
using static ZZZO.Common.API.HlasovaniZastupitele;

namespace ZZZO.Tests;

internal static class Program
{
  internal static int Pocet;
  internal static string Vystupy => Path.Combine(AppContext.BaseDirectory, "test-results");
  [STAThread]
  public static int Main(string[] args)
  {
    int cef = CefSharp.BrowserSubprocess.SelfHost.Main(args);
    if (cef >= 0) return cef;
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
    try
    {
      Directory.CreateDirectory(Vystupy);
      foreach (string log in new[] { "gui-error.txt", "startup-error.txt", "binding-errors.txt" }) File.WriteAllText(Path.Combine(Vystupy, log), "");
      OveritModel();
      OveritGenerator();
      OveritHranicniStavy();
      if (args.Contains("--gui")) return TestyGui.Spustit();
      Console.WriteLine($"OK: {Pocet} kontrol.");
      return 0;
    }
    catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
  }

  internal static void Overit(bool podminka, string popis)
  {
    if (!podminka) throw new InvalidOperationException("TEST SELHAL: " + popis);
    Pocet++;
    Console.WriteLine("OK " + popis);
  }

  internal static Zasedani Ukazka()
  {
    var zas = Zasedani.GenerateSample();
    zas.DatumCasKonani = new DateTime(2026, 8, 20, 18, 0, 0);
    int i = 0;
    foreach (var b in zas.Program.VsechnyBody())
      if (string.IsNullOrWhiteSpace(b.Nadpis)) b.Nadpis = "Bod " + ++i;
    return zas;
  }
  internal static BodProgramu PridatBod(Zasedani zas, string nadpis, RezimBodu rezim)
  {
    var b = new BodProgramu { Nadpis = nadpis };
    b.ZmenitRezim(rezim, zas.Zastupitele);
    zas.Program.BodyProgramu.Add(b);
    return b;
  }

  private static void OveritModel()
  {
    var zas = Ukazka();
    Overit(ValidatorZasedani.Overit(zas).Count == 0, "platný vzor");
    var b = PridatBod(zas, "Investice", RezimBodu.Informativni);
    Overit(zas.Program.VygenerovatBodProgramu(zas, TypBoduProgramu.BodZasedani, true).Nadpis == "Nový bod", "výchozí název nového bodu");
    Overit(zas.Program.VygenerovatBodProgramu(zas, TypBoduProgramu.DoplnenyBodZasedani, true).Nadpis == "Nový doplněný bod", "výchozí název doplněného bodu");
    Overit(b.Usneseni.Count == 0, "informativní bod bez usnesení");
    b.ZmenitRezim(RezimBodu.BereNaVedomi, zas.Zastupitele);
    Overit(b.Usneseni.Count == 0, "bere na vědomí bez usnesení");
    b.ZmenitRezim(RezimBodu.SUsnesenimi, zas.Zastupitele);
    var u = b.Usneseni.Single(); u.TextHtml = "<p>Schvaluje <strong>smlouvu</strong>.</p>";
    Overit(u.VolbyZastupitelu.All(h => h.Volba == VolbaHlasovani.Pro), "výchozí hlas Pro");
    Overit(VyhodnoceniHlasovani.Vyhodnotit(zas, u).Pro == 4, "nepřítomný se nepočítá");
    var pritomni = u.VolbyZastupitelu.Where(h => h.Zastupitel.JePritomen).ToArray();
    pritomni[0].Volba = VolbaHlasovani.Proti;
    pritomni[1].Volba = VolbaHlasovani.ZdrzujeSe;
    Overit(!VyhodnoceniHlasovani.Vyhodnotit(zas, u).JeSchvaleno, "dva hlasy z pěti nestačí");
    pritomni[0].Volba = VolbaHlasovani.Pro;
    Overit(VyhodnoceniHlasovani.Vyhodnotit(zas, u).JeSchvaleno, "tři hlasy z pěti schvalují");
    var pod = new BodProgramu { Nadpis = "Oprava školy" };
    b.Podbody.Add(pod);
    var podU = pod.PridatUsneseni(zas.Zastupitele); podU.TextHtml = "<p>Podbod</p>";
    Overit(zas.Program.RodicBodu(pod) == b, "skutečný rodič podbodu");
    Overit(zas.Program.OcislovatBody()[pod.Id].EndsWith("a. "), "číslování podbodu");
    var novy = new Zastupitel { Jmeno = "Jan", Prijmeni = "Nový", JePritomen = true };
    zas.AddZastupitel(novy);
    Overit(podU.VolbyZastupitelu.Any(h => h.ZastupitelId == novy.Id && h.Pro), "přidaný zastupitel i v podbodu");
    zas.RemoveZastupitel(novy);
    Overit(podU.VolbyZastupitelu.All(h => h.ZastupitelId != novy.Id), "odebraný zastupitel i v podbodu");
    string json = Encoding.UTF8.GetString(zas.ToJson());
    var kopie = Zasedani.NacistJson(json);
    Overit(Encoding.UTF8.GetString(kopie.ToJson()) == json, "bezeztrátový JSON roundtrip");
    Overit(!json.Contains("$ref") && !json.Contains("Vytah"), "JSON bez runtime referencí a odvozených údajů");
    Overit(kopie.Program.VsechnyBody().SelectMany(x => x.Usneseni).SelectMany(x => x.VolbyZastupitelu)
      .All(h => ReferenceEquals(h.Zastupitel, kopie.Zastupitele.Single(z => z.Id == h.ZastupitelId))), "hlasy navázány podle ID");
    bool odmitnuto = false;
    try { Zasedani.NacistJson("{}"); } catch (InvalidDataException) { odmitnuto = true; }
    Overit(odmitnuto, "stará verze souboru je srozumitelně odmítnuta");
    int zmeny = 0;
    using (var sledovani = new SledovaniZasedani(zas, () => zmeny++))
    {
      zas.AdresaKonani.Obec = "Jiná obec";
      podU.TextHtml = "<p>Nový text</p>";
      Overit(zmeny >= 2, "sledování adresy a vnořeného usnesení");
    }
    int pred = zmeny; podU.TextHtml = "<p>Po odpojení</p>";
    Overit(zmeny == pred, "odpojení sledování");
    zas.Zastupitele.First(z => z.JeZapisovatel).JePritomen = false;
    Overit(ValidatorZasedani.Overit(zas).Any(c => c.Kod == "pritomnost"), "validace nepřítomné role");
    Overit(ValidatorZasedani.Overit(zas).Where(c => c.Kod == "pritomnost").All(c => !c.Blokuje(TypDokumentu.Pozvanka)), "role neblokují pozvánku");
    zas.Program.BodyProgramu.Add(zas.Program.VygenerovatBodProgramu(zas, TypBoduProgramu.SchvaleniProgramu));
    Overit(ValidatorZasedani.Overit(zas).Count(c => c.Kod == "duplicita-bodu") == 2, "duplicita speciálního bodu na obou řádcích");
    b.ZmenitRezim(RezimBodu.Informativni, zas.Zastupitele);
    Overit(b.Usneseni.Count == 0 && b.Podbody.Count == 1, "změna režimu nemění podbody");
    string soubor = Path.Combine(Vystupy, "roundtrip.zzzo");
    zas.SaveToFile(soubor); zas.NazevObce = "Změna"; zas.SaveToFile(soubor);
    Overit(Zasedani.LoadFromFile(soubor).NazevObce == "Změna", "atomický přepis souboru");
  }

  private static void OveritHranicniStavy()
  {
    var zas = Ukazka();
    var bod = PridatBod(zas, "Rodič", RezimBodu.SUsnesenimi);
    var usneseni = bod.Usneseni[0]; usneseni.TextHtml = "<p>Text</p>";
    var podbod = new BodProgramu { Nadpis = "Dítě" }; bod.Podbody.Add(podbod);
    podbod.PridatUsneseni(zas.Zastupitele).TextHtml = "<p>Dětské usnesení</p>";
    var kopie = bod.Duplikovat(zas.Zastupitele);
    Overit(kopie.Id != bod.Id && kopie.Podbody[0].Id != podbod.Id && kopie.Usneseni[0].Id != usneseni.Id, "duplikace má vlastní identifikátory");
    Overit(kopie.Usneseni[0].TextHtml == usneseni.TextHtml && kopie.Podbody.Count == 1, "duplikace zachová texty a podbody");
    kopie.Usneseni[0].TextHtml = "<p>Jiná kopie</p>";
    Overit(usneseni.TextHtml == "<p>Text</p>", "kopie nesdílí editovatelná data");
    zas.Program.BodyProgramu.Move(zas.Program.BodyProgramu.IndexOf(bod), 3);
    Overit(zas.Program.OcislovatBody()[podbod.Id] == "1a. ", "přesun rodiče přečísluje i podbod");
    for (int i = 1; i < 27; i++) bod.Podbody.Add(new BodProgramu { Nadpis = "Podbod " + i });
    Overit(zas.Program.OcislovatBody()[bod.Podbody[26].Id] == "1aa. ", "více než 26 podbodů");
    podbod.Podbody.Add(new BodProgramu { Nadpis = "Neplatné zanoření" });
    Overit(ValidatorZasedani.Overit(zas).Any(c => c.Kod == "podbody"), "další zanoření je neplatné");
    bool odmitnuto = false;
    try { Zasedani.NacistJson(Encoding.UTF8.GetString(zas.ToJson())); } catch (InvalidDataException) { odmitnuto = true; }
    Overit(odmitnuto, "načtení odmítne nepodporovanou hloubku");
    podbod.Podbody.Clear();
    var hlas = usneseni.VolbyZastupitelu[0];
    usneseni.VolbyZastupitelu.RemoveAt(0);
    Overit(!VyhodnoceniHlasovani.Vyhodnotit(zas, usneseni).JePlatne, "chybějící hlas");
    usneseni.VolbyZastupitelu.Add(hlas); usneseni.VolbyZastupitelu.Add(hlas);
    Overit(!VyhodnoceniHlasovani.Vyhodnotit(zas, usneseni).JePlatne, "duplicitní hlas");
    usneseni.VolbyZastupitelu.RemoveAt(usneseni.VolbyZastupitelu.Count - 1);
    hlas.Volba = (VolbaHlasovani)99;
    Overit(!VyhodnoceniHlasovani.Vyhodnotit(zas, usneseni).JePlatne, "neznámá volba");
    hlas.Volba = VolbaHlasovani.Pro;
    var id = hlas.ZastupitelId; hlas.ZastupitelId = Guid.NewGuid();
    Overit(!VyhodnoceniHlasovani.Vyhodnotit(zas, usneseni).JePlatne, "hlas neznámé osoby");
    hlas.ZastupitelId = id;
    foreach (var z in zas.Zastupitele) z.JePritomen = false;
    Overit(!VyhodnoceniHlasovani.Vyhodnotit(zas, usneseni).JeSchvaleno, "nikdo přítomen");
    usneseni.TextHtml = "<p>&nbsp;<br></p>";
    Overit(ValidatorZasedani.Overit(zas).Any(c => c.Kod == "text-usneseni" && c.PolozkaId == usneseni.Id), "vizuálně prázdné HTML");
    bod.Usneseni.Clear();
    Overit(ValidatorZasedani.Overit(zas).Any(c => c.Kod == "prazdna-usneseni"), "režim usnesení bez návrhů je rozpracovaný");
    zas.SaveToFile(Path.Combine(Vystupy, "rozpracovany.zzzo"));
    Overit(Zasedani.LoadFromFile(Path.Combine(Vystupy, "rozpracovany.zzzo")).Program.BodyProgramu.Count == zas.Program.BodyProgramu.Count, "rozpracovaná data lze uložit");
    var prazdne = Zasedani.VytvoritNove();
    Overit(prazdne.Zastupitele.Count == 0 && prazdne.Program.BodyProgramu.Count == 3, "nové zasedání bez fiktivních osob");
  }

  private static void OveritGenerator()
  {
    var zas = Ukazka();
    var info = PridatBod(zas, "INFORMACE & <text>", RezimBodu.Informativni);
    info.PrubehHtml = "<p>Text &amp; &lt;doslova&gt; <strong>tučně</strong>.</p>";
    var vedomi = PridatBod(zas, "VZETÍ NA VĚDOMÍ", RezimBodu.BereNaVedomi);
    var s = PridatBod(zas, "ROZHODNUTÍ", RezimBodu.SUsnesenimi);
    s.Usneseni[0].TextHtml = "<p>Schvaluje <strong>smlouvu</strong> &amp; dodatek.</p><ul><li>Podmínka</li></ul>";
    s.PridatUsneseni(zas.Zastupitele).TextHtml = "<p>Schvaluje rozpočet.</p>";
    var pod = new BodProgramu { Nadpis = "PODBOD" }; s.Podbody.Add(pod);
    pod.PridatUsneseni(zas.Zastupitele).TextHtml = "<p>Podbodové usnesení.</p>";
    string pred = Encoding.UTF8.GetString(zas.ToJson());
    var gen = new GeneratorHtml();
    byte[] data = gen.Generate(zas, new Progress<int>(), new GeneratorHtmlParams { HtmlStyle = "svésedlice.css", KindOfDocument = TypDokumentu.Zapis }).GetAwaiter().GetResult();
    string html = Encoding.UTF8.GetString(data);
    Overit(Encoding.UTF8.GetString(zas.ToJson()) == pred, "generátor nemění vstupní data");
    Overit(!html.Contains("Bez znaku", StringComparison.OrdinalIgnoreCase), "GUI placeholder znaku není ve výstupním HTML");
    var doc = new HtmlDocument(); doc.LoadHtml(html);
    string text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
    Overit(text.Split("bere na vědomí").Length - 1 == 1, "bere na vědomí pouze explicitně a jednou");
    Overit(doc.DocumentNode.SelectNodes("//h3")?.Count == 1, "podbody mají nadpis h3");
    Overit(doc.DocumentNode.SelectNodes("//strong")?.Count >= 3, "HTML usnesení zachováno v zápisu i přehledu");
    Overit(doc.DocumentNode.SelectNodes("//div[@class='signature']/hr")?.Count == 3, "podpisové čáry jsou HTML, nikoli viditelný kód");
    Overit(!html.Contains("&lt;hr") && html.Contains("&lt;doslova&gt;"), "bez globálního HTML unescape");
    Overit(doc.DocumentNode.SelectSingleNode("//style").InnerText == GeneratorHtml.GetStyle("svésedlice.css"), "původní CSS vloženo beze změny");
    Overit(html.Contains("resolution-vote-box success") && html.Contains("resolution-decision-success"), "původní stylové třídy hlasování");
    Overit(html.Contains("Celkový počet přijatých usnesení je 3."), "přehled zahrnuje všechna běžná usnesení i podbod");
    File.WriteAllBytes(Path.Combine(Vystupy, "zapis.html"), data);
    var pozvanka = gen.Generate(zas, null, new GeneratorHtmlParams { HtmlStyle = "svésedlice.css", KindOfDocument = TypDokumentu.Pozvanka }).GetAwaiter().GetResult();
    doc.LoadHtml(Encoding.UTF8.GetString(pozvanka));
    Overit(doc.DocumentNode.SelectSingleNode("//li/ol/li") != null, "pozvánka má platné zanoření seznamu");
    File.WriteAllBytes(Path.Combine(Vystupy, "pozvanka.html"), pozvanka);
    foreach (var b in zas.Program.BodyProgramu.Where(b => !b.JeBezny).ToArray()) zas.Program.BodyProgramu.Remove(b);
    Overit(gen.Generate(zas, null, new GeneratorHtmlParams { KindOfDocument = TypDokumentu.Pozvanka }).GetAwaiter().GetResult().Length > 0, "pozvánka bez procedurálních bodů");
    bool blokovano = false;
    try { gen.Generate(zas, null, new GeneratorHtmlParams { KindOfDocument = TypDokumentu.Zapis }).GetAwaiter().GetResult(); }
    catch (InvalidOperationException) { blokovano = true; }
    Overit(blokovano, "zápis blokován chybějícími speciálními body");
    var xml = new XmlDocument(); var cil = xml.CreateElement("div"); xml.AppendChild(cil);
    HtmlObsah.Vlozit(cil, "<script>alert(1)</script><p onclick='x()'><a href='javascript:alert(1)'>A</a><strong>B</strong>&lt;x&gt;</p>");
    Overit(!cil.OuterXml.Contains("script") && !cil.OuterXml.Contains("onclick") && cil.OuterXml.Contains("<strong>B</strong>"), "bezpečné vkládání HTML");
  }
}
