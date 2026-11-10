using System.IO;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ZZZO.Common.Generators;

namespace ZZZO.Tests;

internal static class TestyTabulekDocx
{
  internal static void Spustit(byte[] template)
  {
    XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    using var document = new DokumentDocx(template);
    var converter = new ObsahDocx(document);
    var all = new List<XElement>();
    XElement Check(string html, double[] proportions, string message, double tableFraction = 1)
    {
      var table = converter.Convert(html).Single(t => t.Name == w + "tbl");
      int[] widths = table.Element(w + "tblGrid").Elements().Select(c => (int)c.Attribute(w + "w")).ToArray();
      int total = widths.Sum();
      Program.Overit(widths.Length == proportions.Length && widths.Select((v, i) => Math.Abs((double)v / total - proportions[i])).All(d => d < .001), message);
      Program.Overit(Math.Abs(total - document.ContentWidth * tableFraction) <= 2 &&
        table.Elements(w + "tr").All(r => r.Elements(w + "tc").Sum(c => (int)c.Element(w + "tcPr").Element(w + "tcW").Attribute(w + "w")) == total),
        "šířky buněk a tabulky souhlasí: " + message);
      all.Add(table);
      return table;
    }
    Check("<table style='width:100%'><colgroup><col style='width:20%'><col style='width:50%'><col style='width:30%'></colgroup><tr><td>A</td><td>B</td><td>C</td></tr></table>",
      new[] { .2, .5, .3 }, "procentní šířky z TinyMCE");
    Check("<table><colgroup><col width='96'><col style='width:72pt'><col></colgroup><tr><td>A</td><td>B</td><td>C</td></tr></table>",
      new[] { 1440.0 / document.ContentWidth, 1440.0 / document.ContentWidth, (document.ContentWidth - 2880.0) / document.ContentWidth }, "kombinace px, pt a neurčeného sloupce");
    Check("<table style='width:50%'><tr><th colspan='3' style='width:100%'>Záhlaví</th></tr><tr><td style='width:20%'>A</td><td style='width:50%'>B</td><td style='width:30%'>C</td></tr></table>",
      new[] { .2, .5, .3 }, "šířky buněk pod sloučeným záhlavím", .5);
    var vertical = Check("<table><tr><td rowspan='2'>A</td><td>B</td></tr><tr><td>C</td><td>D</td></tr></table>",
      new[] { 1.0 / 3, 1.0 / 3, 1.0 / 3 }, "rowspan se započítá do skutečné mřížky");
    Program.Overit(vertical.Descendants(w + "t").Any(t => t.Value == "D") && vertical.Descendants(w + "vMerge").Count() == 2, "svislé sloučení neztratí poslední buňku dalšího řádku");
    var merged = Check("<table><colgroup><col width='10%'><col width='20%'><col width='70%'></colgroup><tr><td colspan='2' rowspan='2'>A</td><td>B</td></tr><tr><td>C</td></tr></table>",
      new[] { .1, .2, .7 }, "kombinované vodorovné a svislé sloučení");
    Program.Overit(merged.Elements(w + "tr").All(r => r.Element(w + "tc").Element(w + "tcPr").Element(w + "gridSpan")?.Attribute(w + "val")?.Value == "2"),
      "pokračování sloučené buňky zachová colspan a součet šířek");
    Check("<table width='2000'><colgroup><col width='1600'><col width='400'></colgroup><tr><td>A</td><td>B</td></tr></table>",
      new[] { .8, .2 }, "příliš široká tabulka se vejde do stránky při zachování poměru");
    Check("<table><colgroup><col span='2' width='20%'><col width='60%'></colgroup><tr><td>A</td><td>B</td><td>C</td></tr></table>",
      new[] { .2, .2, .6 }, "col span z Excelu zachová šířku každého sloupce");
    var nested = Check("<table><tr><td style='width:30%'><table style='width:100%'><tr><td>Vnořený obsah</td></tr></table></td><td style='width:70%'>B</td></tr></table>",
      new[] { .3, .7 }, "základní šířky vnější tabulky");
    Program.Overit((int)nested.Descendants(w + "tbl").Single().Element(w + "tblPr").Element(w + "tblW").Attribute(w + "w") <
      (int)nested.Element(w + "tr").Element(w + "tc").Element(w + "tcPr").Element(w + "tcW").Attribute(w + "w"), "vnořená starší tabulka nepřesáhne svou buňku");
    document.Replace("Body", all);
    byte[] output = document.Save();
    using var stream = new MemoryStream(output);
    using var package = WordprocessingDocument.Open(stream, false);
    var errors = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2019).Validate(package).ToList();
    foreach (var error in errors) Console.WriteLine(error.Description);
    Program.Overit(errors.Count == 0, "všechny varianty mřížek mají validní OOXML");
    File.WriteAllBytes(Path.Combine(Program.Vystupy, "tabulky.docx"), output);
  }
}
