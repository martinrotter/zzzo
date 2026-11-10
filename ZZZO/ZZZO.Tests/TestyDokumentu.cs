using System.IO;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ZZZO.Common.API;
using ZZZO.Common.Generators;
using static ZZZO.Tests.Program;
using static ZZZO.Common.Generators.Generator;

namespace ZZZO.Tests;

internal static class TestyDokumentu
{
  private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
  private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
  public static void Spustit(bool pdf)
  {
    var zas = Ukazka();
    var info = PridatBod(zas, "INFORMACE & <text>", RezimBodu.Informativni);
    info.PrubehHtml = "<p style='text-align:center'>Text &amp; &lt;doslova&gt; <strong>tučně</strong> <em>kurzívou</em> <u>podtrženě</u> x<sup>2</sup> H<sub>2</sub>O.</p>" +
      "<ol start='3'><li>První<ol type='a'><li>Vnořená</li></ol></li><li>Druhá</li></ol>" +
      "<table><thead><tr><th colspan='2'>Záhlaví</th></tr></thead><tbody><tr><td rowspan='2'>Sloučená</td><td>Buňka</td></tr><tr><td>Poslední</td></tr></tbody></table>" +
      "<p><a href='https://example.org/'>Odkaz</a><img width='20' src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jM1kAAAAASUVORK5CYII='></p>" +
      "<script>nezobrazovat</script><p><a href='javascript:alert(1)' onclick='x()'>Bezpečný text</a></p>";
    PridatBod(zas, "VZETÍ NA VĚDOMÍ", RezimBodu.BereNaVedomi);
    var decision = PridatBod(zas, "ROZHODNUTÍ", RezimBodu.SUsnesenimi);
    decision.Usneseni[0].TextHtml = "<p>Schvaluje <strong>smlouvu</strong> &amp; dodatek.</p><ul><li>Podmínka</li></ul>";
    decision.PridatUsneseni(zas.Zastupitele).TextHtml = "<p>Schvaluje rozpočet.</p>";
    var child = new BodProgramu { Nadpis = "PODBOD" }; decision.Podbody.Add(child);
    child.PridatUsneseni(zas.Zastupitele).TextHtml = "<p>Podbodové usnesení.</p>";
    var rejected = PridatBod(zas, "ZAMÍTNUTÍ", RezimBodu.SUsnesenimi);
    rejected.Usneseni[0].TextHtml = "<p>Zamítnutý návrh</p>";
    foreach (var vote in rejected.Usneseni[0].VolbyZastupitelu) vote.Volba = HlasovaniZastupitele.VolbaHlasovani.Proti;
    string before = System.Text.Encoding.UTF8.GetString(zas.ToJson());
    var generator = new GeneratorDocx();
    byte[] template = GeneratorDocx.ReadTemplate(generator.Templates.First());
    Validate(template, "šablona");
    TestyTabulekDocx.Spustit(template);
    using (var broken = new MemoryStream())
    {
      broken.Write(template); broken.Position = 0;
      using (var package = WordprocessingDocument.Open(broken, true))
        package.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Tag>().First().Val = "ZZZO.Chybejici";
      bool rejectedTemplate = false;
      try { generator.Generate(zas, null, new GeneratorDocxParams { TemplateData = broken.ToArray() }).GetAwaiter().GetResult(); }
      catch (InvalidDataException) { rejectedTemplate = true; }
      Overit(rejectedTemplate, "neúplná šablona se odmítne bez tiché ztráty obsahu");
    }
    byte[] data = generator.Generate(zas, null).GetAwaiter().GetResult();
    Validate(data, "zápis");
    Overit(System.Text.Encoding.UTF8.GetString(zas.ToJson()) == before, "generátor nemění data");
    using (var stream = new MemoryStream(data))
    using (var doc = WordprocessingDocument.Open(stream, false))
    {
      var main = doc.MainDocumentPart;
      var body = XDocument.Load(main.GetStream());
      string text = string.Concat(body.Descendants(W + "t").Select(t => t.Value));
      Overit(text.Split("bere na vědomí").Length - 1 == 1, "bere na vědomí pouze jednou");
      Overit(body.Descendants(W + "pStyle").Any(e => (string)e.Attribute(W + "val") == "Heading2"), "podbody mají vlastní styl");
      Overit(text.Contains("Celkový počet přijatých usnesení je 3."), "souhrn čítá běžná usnesení i podbody");
      var summary = body.Descendants(W + "p").First(p => p.Descendants(W + "t").Any(t => t.Value == "Přijatá usnesení"));
      Overit(!string.Concat(summary.ElementsAfterSelf().Descendants(W + "t").Select(t => t.Value)).Contains("Zamítnutý návrh"), "zamítnutý návrh není v přijatých usneseních");
      Overit(text.Contains("& <doslova>") && !text.Contains("nezobrazovat") && !text.Contains("Bez znaku"), "entity, skripty a placeholder znaku");
      Overit(body.Descendants(W + "b").Count() >= 3 && body.Descendants(W + "i").Any() && body.Descendants(W + "u").Any() && body.Descendants(W + "vertAlign").Count() == 2, "formátování TinyMCE je nativní Word");
      Overit(body.Descendants(W + "numPr").Any() && main.NumberingDefinitionsPart.Numbering.InnerXml.Contains("lowerLetter"), "nativní číslované i vnořené seznamy Wordu");
      Overit(body.Descendants(W + "gridSpan").Any() && body.Descendants(W + "vMerge").Count() == 2, "sloučené buňky tabulky");
      Overit(main.ImageParts.Any() && main.HyperlinkRelationships.Single().Uri.AbsoluteUri == "https://example.org/", "vložený obrázek a bezpečný odkaz");
      Overit(!body.Descendants(W + "altChunk").Any() && !body.Descendants(W + "sdt").Any(), "dokument neobsahuje HTML ani nevyplněné bloky");
      Overit(body.Descendants(W + "tblStyle").Any(e => (string)e.Attribute(W + "val") == "VoteRejected"), "zamítnuté hlasování má vlastní styl");
      var fonts = XDocument.Load(main.FontTablePart.GetStream());
      int embedded = 0;
      foreach (var font in fonts.Descendants().Where(e => e.Name.LocalName.StartsWith("embed", StringComparison.Ordinal)))
      {
        byte[] mask = Convert.FromHexString(((string)font.Attribute(W + "fontKey")).Replace("-", "").Trim('{', '}')).Reverse().ToArray();
        using var source = main.FontTablePart.GetPartById((string)font.Attribute(R + "id")).GetStream();
        using var copy = new MemoryStream(); source.CopyTo(copy); byte[] bytes = copy.ToArray();
        for (int i = 0; i < 32; i++) bytes[i] ^= mask[i % 16];
        string family = (string)font.Parent.Attribute(W + "name");
        string face = font.Name.LocalName switch { "embedBold" => "Bold", "embedItalic" => "Italic", "embedBoldItalic" => "BoldItalic", _ => "Regular" };
        if (family.EndsWith("SemiBold")) face = face == "Italic" ? "SemiBoldItalic" : "SemiBold";
        string filename = family == "Noto Sans Symbols 2" ? "NotoSansSymbols2-Regular.ttf" : "EBGaramond-" + face + ".ttf";
        Overit(bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", filename))), "vložený font odpovídá celému zdrojovému TTF"); embedded++;
      }
      Overit(embedded == 7, "vloženo šest řezů EB Garamond a font symbolů");
      Overit(main.FooterParts.Any(p => p.Footer.InnerXml.Contains("NUMPAGES")), "patička obsahuje pole počtu stran");
    }
    File.WriteAllBytes(Path.Combine(Vystupy, "zapis.docx"), data);
    byte[] invitation = generator.Generate(zas, null, new GeneratorDocxParams { KindOfDocument = TypDokumentu.Pozvanka }).GetAwaiter().GetResult();
    Validate(invitation, "pozvánka");
    File.WriteAllBytes(Path.Combine(Vystupy, "pozvanka.docx"), invitation);
    var logoZas = Ukazka();
    var pixels = System.Windows.Media.Imaging.BitmapSource.Create(80, 120, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null, new byte[80 * 120 * 3], 80 * 3);
    var png = new System.Windows.Media.Imaging.PngBitmapEncoder(); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(pixels));
    using (var logoStream = new MemoryStream()) { png.Save(logoStream); logoZas.LogoObceData = logoStream.ToArray(); }
    foreach (var kind in new[] { TypDokumentu.Zapis, TypDokumentu.Pozvanka })
    {
      byte[] withLogo = generator.Generate(logoZas, null, new GeneratorDocxParams { KindOfDocument = kind }).GetAwaiter().GetResult();
      using var stream = new MemoryStream(withLogo);
      using var package = WordprocessingDocument.Open(stream, false);
      XNamespace drawing = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
      var extent = XDocument.Load(package.MainDocumentPart.GetStream()).Descendants(drawing + "extent").Single();
      long width = (long)extent.Attribute("cx"), height = (long)extent.Attribute("cy");
      Overit(Math.Abs(width / 360000.0 - 2.534) < 0.001 && Math.Abs((double)height / width - 1.5) < 0.001,
        "logo ze zasedání má původní šířku a poměr stran " + kind);
    }
    foreach (var entry in zas.Program.BodyProgramu.Where(b => !b.JeBezny).ToArray()) zas.Program.BodyProgramu.Remove(entry);
    Overit(generator.Generate(zas, null, new GeneratorDocxParams { KindOfDocument = TypDokumentu.Pozvanka }).GetAwaiter().GetResult().Length > 0, "pozvánka bez procedurálních bodů");
    bool blocked = false;
    try { generator.Generate(zas, null).GetAwaiter().GetResult(); } catch (InvalidOperationException) { blocked = true; }
    Overit(blocked, "zápis blokuje chybějící procedurální body");
    if (pdf)
    {
      foreach (var item in new[] { ("zapis", data), ("pozvanka", invitation) })
      {
        var converter = new PrevodnikPdf();
        byte[] output = converter.PrevestAsync(item.Item2).GetAwaiter().GetResult();
        File.WriteAllBytes(Path.Combine(Vystupy, item.Item1 + ".pdf"), output);
        using var pdfStream = new MemoryStream(output, false);
        using var reader = PdfiumViewer.Core.PdfDocument.Load(pdfStream);
        Overit(reader.PageCount > 0 && reader.Pages[0].GetText().Contains(zas.NazevObce), "skutečný převod DOCX → PDF " + item.Item1);
        if (PrevodnikWord.JeDostupny && !string.Equals(Environment.GetEnvironmentVariable("ZZZO_PDF_CONVERTER"), "libreoffice", StringComparison.OrdinalIgnoreCase))
          Overit(converter.PouzityPrevodnik == "Microsoft Word", "nainstalovaný Word má přednost " + item.Item1);
      }
      var fallback = new PrevodnikPdf((input, output, work, token) =>
      {
        File.WriteAllText(output, "nedokončené PDF");
        throw new IOException("Testovaná chyba Wordu.");
      });
      byte[] fallbackPdf = fallback.PrevestAsync(data).GetAwaiter().GetResult();
      File.WriteAllBytes(Path.Combine(Vystupy, "zapis-libreoffice.pdf"), fallbackPdf);
      using var fallbackStream = new MemoryStream(fallbackPdf, false);
      using var fallbackReader = PdfiumViewer.Core.PdfDocument.Load(fallbackStream);
      Overit(fallback.PouzityPrevodnik == "LibreOffice" && fallbackReader.Pages[0].GetText().Contains(zas.NazevObce),
        "chyba Wordu včetně nedokončeného PDF přejde na skutečný LibreOffice");
    }
  }

  private static void Validate(byte[] bytes, string name)
  {
    using var stream = new MemoryStream(bytes);
    using var doc = WordprocessingDocument.Open(stream, false);
    var errors = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2019).Validate(doc).ToList();
    foreach (var error in errors) Console.WriteLine($"OOXML {error.Part?.Uri} {error.Path?.XPath}: {error.Description}");
    Overit(errors.Count == 0, "validní OOXML " + name);
  }
}
