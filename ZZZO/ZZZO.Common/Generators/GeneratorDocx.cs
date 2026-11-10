using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using ZZZO.Common.API;
using ZZZO.Common.Validace;
using static ZZZO.Common.Generators.DokumentDocx;

namespace ZZZO.Common.Generators;

public sealed class GeneratorDocxParams
{
  public string Template { get; init; }
  public byte[] TemplateData { get; init; }
  public Generator.TypDokumentu KindOfDocument { get; init; } = Generator.TypDokumentu.Zapis;
}

public sealed class GeneratorDocx : Generator
{
  public override string FileSuffix => "docx";
  public override string Title => "DOCX";
  public List<TypDokumentu> KindsOfDocuments { get; } = new() { TypDokumentu.Pozvanka, TypDokumentu.Zapis };
  public List<string> Templates => Directory.Exists(Constants.PathsAndFiles.AppTemplatesFolder)
    ? Directory.GetFiles(Constants.PathsAndFiles.AppTemplatesFolder, "*.docx").Select(Path.GetFileName).OrderBy(n => n).ToList() : new();

  public static byte[] ReadTemplate(string name)
  {
    // The persisted setting is a filename within the application's template directory.
    if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name)) throw new InvalidDataException("Vyberte DOCX šablonu z adresáře Data/Templates.");
    return File.ReadAllBytes(Path.Combine(Constants.PathsAndFiles.AppTemplatesFolder, name));
  }
  public static string TemplateHash(string name) => Convert.ToHexString(SHA256.HashData(ReadTemplate(name)));

  protected override byte[] GenerateDoWork(Zasedani zas, IProgress<int> progress, object param)
  {
    var options = param as GeneratorDocxParams ?? new();
    var errors = ValidatorZasedani.Overit(zas).Where(c => c.Blokuje(options.KindOfDocument)).ToList();
    if (errors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(e => e.Zprava)));
    byte[] template = options.TemplateData ?? ReadTemplate(options.Template ?? Templates.FirstOrDefault());
    using var doc = new DokumentDocx(template);
    var rich = new ObsahDocx(doc);
    bool record = options.KindOfDocument == TypDokumentu.Zapis;
    progress?.Report(10);
    byte[] logo = zas.LogoObceData;
    doc.Replace("Logo", logo == null ? Array.Empty<XElement>() : new[] { Paragraph(new[] { doc.Image(logo, 2.534, 96) }, "Logo") });
    string title = record
      ? $"Zápis z {zas.Poradi}. zasedání zastupitelstva obce {zas.NazevObce} konaného dne {zas.DatumCasKonani:d. M. yyyy}"
      : $"Pozvánka na {zas.Poradi}. zasedání zastupitelstva obce {zas.NazevObce}";
    doc.Replace("Title", new[] { P(title, "Title") });
    doc.SetTitle(title);
    var opening = new List<XElement>();
    var body = new List<XElement>();
    var summary = new List<XElement>();
    var signatures = new List<XElement>();
    if (!record)
    {
      opening.Add(P($"Starosta obce {zas.NazevObce} podle § 103 odst. 5 zákona č. 128/2000 Sb. o obcích svolává {zas.Poradi}. zasedání zastupitelstva obce {zas.NazevObce}."));
      opening.Add(doc.Table("VoteAccepted", new[] { new[] { new XElement("cell",
        P($"Datum konání: {zas.DatumCasKonani:dddd, d. M. yyyy v H:mm} SEČ"),
        P($"Místo konání: {zas.AdresaKonani}, v {zas.AdresaKonani.PopisMista}")) } }, new[] { 7182 }));
      body.Add(P("Navržený program", "Heading1"));
      body.AddRange(Agenda(zas));
    }
    else
    {
      var chair = zas.Zastupitele.Single(z => z.JeRidici);
      var mayor = zas.Zastupitele.Single(z => z.JeStarosta);
      var recorder = zas.Zastupitele.Single(z => z.JeZapisovatel);
      var verifiers = zas.Zastupitele.Where(z => z.JeOverovatel).ToList();
      opening.Add(P("Zahájení", "Heading1"));
      opening.Add(P($"Zasedání zastupitelstva obce (dále jen ZO) {zas.NazevObce} bylo zahájeno dne {zas.DatumCasKonani:d. M. yyyy v H:mm} SEČ na adrese {zas.AdresaKonani} v {zas.AdresaKonani.PopisMista}."));
      opening.Add(P("Zúčastnění zastupitelé: " + string.Join(", ", zas.Zastupitele.Where(z => z.JePritomen).OrderBy(z => z.Prijmeni).Select(Name)) + "."));
      string absent = string.Join(", ", zas.Zastupitele.Where(z => !z.JePritomen).OrderBy(z => z.Prijmeni).Select(Name));
      opening.Add(P("Nepřítomní zastupitelé: " + (absent.Length == 0 ? "-" : absent + ".")));
      opening.Add(P($"Zasedání ZO {Decline("navštívil", "navštívili", "navštívilo", zas.PocetHostu)} {zas.PocetHostu} {Decline("host", "hosté", "hostů", zas.PocetHostu)} z řad veřejnosti."));
      opening.Add(P($"Zasedání ZO řídil pan {Name(chair)}."));
      opening.Add(P($"Všechna hlasování na tomto zasedání ZO {zas.NazevObce} jsou veřejná a zastupitelé hlasují zdvižením ruky."));
      var numbers = zas.Program.OcislovatBody();
      int resolutionNumber = 0, acceptedCount = 0;
      foreach (var parent in zas.Program.BodyProgramu)
      {
        Entry(parent, false);
        foreach (var child in parent.Podbody) Entry(child, true);
      }
      void Entry(BodProgramu entry, bool child)
      {
        body.Add(P(numbers.GetValueOrDefault(entry.Id, "") + entry.Nadpis + Added(entry), child ? "Heading2" : "Heading1"));
        if (entry.Typ == BodProgramu.TypBoduProgramu.SchvaleniZapisOver)
          body.Add(P($"Řídící osoba zasedání ZO {zas.NazevObce} navrhla, aby zapisovatelem byl {Name(recorder)} a ověřovateli zápisu byli {string.Join(" a ", verifiers.Select(Name))}."));
        if (entry.Typ == BodProgramu.TypBoduProgramu.SchvaleniProgramu)
        {
          body.AddRange(Agenda(zas));
          body.Add(P($"Řídící osoba zasedání ZO {zas.NazevObce} navrhla schválit výše uvedený návrh programu.", "AfterAgenda"));
        }
        body.AddRange(rich.Convert(entry.PrubehHtml));
        if (entry.Rezim == RezimBodu.BereNaVedomi) body.Add(P($"ZO {zas.NazevObce} bere na vědomí.", "Resolution"));
        if (entry.Rezim != RezimBodu.SUsnesenimi) return;
        foreach (var resolution in entry.Usneseni)
        {
          string prefix = entry.JeBezny ? $"Č. {++resolutionNumber}-{zas.DatumCasKonani.Month}/{zas.DatumCasKonani.Year} - " : "";
          if (entry.JeBezny) body.Add(P("Návrh usnesení:"));
          var text = rich.Convert(resolution.TextHtml, "Resolution", prefix);
          body.AddRange(text);
          var vote = VyhodnoceniHlasovani.Vyhodnotit(zas, resolution);
          if (entry.JeBezny && vote.JeSchvaleno) { summary.AddRange(text.Select(e => new XElement(e))); acceptedCount++; }
          string heading = entry.Typ switch
          {
            BodProgramu.TypBoduProgramu.SchvaleniZapisOver => "Hlasování o navrženém zapisovateli a ověřovatelích zápisu:",
            BodProgramu.TypBoduProgramu.SchvaleniProgramu => "Hlasování o návrhu programu:",
            _ => "Hlasování o návrhu usnesení:"
          };
          var contents = new XElement("cell", P(heading, "VoteHeading"));
          foreach (var (choice, label, color) in new[] {
            (HlasovaniZastupitele.VolbaHlasovani.Pro, "✓PRO", "32CD32"),
            (HlasovaniZastupitele.VolbaHlasovani.Proti, "×PROTI", "FF0000"),
            (HlasovaniZastupitele.VolbaHlasovani.ZdrzujeSe, "?ZDRŽUJE SE", "FFA500") })
          {
            var voters = resolution.VolbyZastupitelu.Where(v => v.Zastupitel.JePritomen && v.Volba == choice).Select(v => Name(v.Zastupitel)).ToList();
            string names = voters.Count > 0 && voters.Count < zas.Zastupitele.Count(z => z.JePritomen) ? $" ({string.Join(", ", voters)})" : "";
            contents.Add(Paragraph(new[] { Symbol(label[..1], color), new XElement(W + "r", new XElement(W + "tab")), Run(label[1..] + $": {voters.Count}" + names) }, "VoteLine"));
          }
          // A separate narrow cell matches the decision box without stretching its border across the vote block.
          contents.Add(P("\u00a0", "Vote"));
          var decisionText = Paragraph(new[] { Symbol(vote.JeSchvaleno ? "✓ " : "× ", vote.JeSchvaleno ? "32CD32" : "FF0000"), Run(vote.Vysledek) }, vote.JeSchvaleno ? "DecisionAccepted" : "DecisionRejected");
          string boxStyle = vote.JeSchvaleno ? "DecisionBoxAccepted" : "DecisionBoxRejected";
          contents.Add(doc.Table(boxStyle, new[] { new[] { decisionText } }, new[] { doc.DecisionWidth(decisionText, boxStyle) }));
          contents.Add(P("", "TableEnd"));
          var voteTable = doc.Table(vote.JeSchvaleno ? "VoteAccepted" : "VoteRejected", new[] { new[] { contents } }, new[] { 7182 });
          foreach (var p in voteTable.Descendants(W + "p").SkipLast(1)) p.Element(W + "pPr").Add(new XElement(W + "keepNext"));
          foreach (var row in voteTable.Descendants(W + "tr")) row.AddFirst(new XElement(W + "trPr", new XElement(W + "cantSplit")));
          body.Add(voteTable);
          body.Add(P("", "Vote"));
        }
      }
      summary.Insert(0, P("Přijatá usnesení", "SummaryTitle"));
      summary.Add(P($"Celkový počet přijatých usnesení je {acceptedCount}."));
      var people = new[] { (mayor, "starosta obce") }.Concat(verifiers.Select(z => (z, "ověřovatel zápisu"))).ToList();
      // Keep three signatures per row, with additional verifiers on subsequent rows.
      foreach (var group in people.Chunk(3))
      {
        var row = group.Select(person => new XElement("cell", P("………………………………", "Signature"),
          Paragraph(new[] { Run(Name(person.Item1)), new XElement(W + "r", new XElement(W + "br")), Run(person.Item2) }, "Vote"))).ToArray();
        foreach (var p in row.SelectMany(e => e.Elements())) p.Element(W + "pPr").Add(Val("jc", "center"), new XElement(W + "keepNext"));
        signatures.Add(doc.Table("Signatures", new[] { row }, Enumerable.Repeat(10260 / row.Length, row.Length).ToArray()));
      }
    }
    progress?.Report(80);
    doc.Replace("Opening", opening); doc.Replace("Body", body); doc.Replace("Resolutions", summary); doc.Replace("Signatures", signatures);
    // Word requires a final paragraph after a table and before section properties.
    if (doc.Body.Elements().LastOrDefault(e => e.Name != W + "sectPr")?.Name == W + "tbl")
      doc.Body.Element(W + "sectPr").AddBeforeSelf(P(""));
    byte[] output = doc.Save(); progress?.Report(100); return output;
  }

  private static IEnumerable<XElement> Agenda(Zasedani zas)
  {
    var numbers = zas.Program.OcislovatBody();
    foreach (var parent in zas.Program.BodyProgramu.Where(b => b.JeBezny))
    {
      yield return P(numbers.GetValueOrDefault(parent.Id, "") + parent.Nadpis + Added(parent), "Agenda");
      foreach (var child in parent.Podbody) yield return P(numbers.GetValueOrDefault(child.Id, "") + child.Nadpis + Added(child), "Subagenda");
    }
  }
  private static string Name(Zastupitel z) => z.Jmeno + " " + z.Prijmeni;
  private static string Added(BodProgramu b) => b.Typ == BodProgramu.TypBoduProgramu.DoplnenyBodZasedani ? " (doplněný bod programu)" : "";
  private static string Decline(string one, string few, string many, int count) => count == 1 ? one : count is >= 2 and <= 4 ? few : many;
}
