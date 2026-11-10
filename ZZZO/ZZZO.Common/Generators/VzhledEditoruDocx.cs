using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace ZZZO.Common.Generators;

/// <summary>Shares paragraph and table geometry with DOCX; the editor keeps its own UI font.</summary>
public static class VzhledEditoruDocx
{
  public static string VytvoritCss(byte[] template, string paragraphStyle)
  {
    XNamespace w = DokumentDocx.W;
    using var input = new MemoryStream(template, false);
    using var package = WordprocessingDocument.Open(input, false);
    using var source = (package.MainDocumentPart?.StyleDefinitionsPart ?? throw new InvalidDataException("V šabloně chybí styly.")).GetStream();
    var root = XDocument.Load(source).Root;
    var styles = root.Elements(w + "style").ToDictionary(s => (string)s.Attribute(w + "styleId"));
    using var documentSource = package.MainDocumentPart.GetStream();
    var section = XDocument.Load(documentSource).Root?.Element(w + "body")?.Element(w + "sectPr");
    var margins = section?.Element(w + "pgMar");
    int contentWidth = ((int?)section?.Element(w + "pgSz")?.Attribute(w + "w") ?? 11906)
      - ((int?)margins?.Attribute(w + "left") ?? 823) - ((int?)margins?.Attribute(w + "right") ?? 823);
    IEnumerable<XElement> Definitions(string style, string group)
    {
      var seen = new HashSet<string>();
      while (style != null && seen.Add(style) && styles.TryGetValue(style, out var definition))
      {
        if (definition.Element(w + group) is XElement properties) yield return properties;
        style = (string)definition.Element(w + "basedOn")?.Attribute(w + "val");
      }
      var defaults = root.Element(w + "docDefaults")?.Element(w + (group + "Default"))?.Element(w + group);
      if (defaults != null) yield return defaults;
    }
    string Attribute(string style, string group, string property, string attribute) => Definitions(style, group)
      .Select(p => (string)p.Element(w + property)?.Attribute(w + attribute)).FirstOrDefault(a => a != null);
    double Number(string value, double fallback) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) ? n : fallback;
    string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    string Spacing(string style) => $"margin-top:{N(Number(Attribute(style, "pPr", "spacing", "before"), 0) / 20)}pt;" +
        $"margin-bottom:{N(Number(Attribute(style, "pPr", "spacing", "after"), 0) / 20)}pt;";
    string alignment = Attribute(paragraphStyle, "pPr", "jc", "val") switch
    { "both" or "distribute" => "justify", "center" => "center", "right" or "end" => "right", _ => "left" };
    var css = new StringBuilder();
    css.Append("body{text-align:").Append(alignment)
      .Append($";max-width:{N(Math.Max(1, contentWidth) / 20.0)}pt;}}");
    css.Append("p{").Append(Spacing(paragraphStyle)).Append("}");
    css.Append("td,th{text-align:left;vertical-align:top;}");
    css.Append("td p,th p{").Append(Spacing("Normal")).Append("}th{font-weight:bold;}");
    css.Append("h1,h2{").Append(Spacing("Heading1")).Append("}");
    css.Append("h3,h4,h5,h6{").Append(Spacing("Heading2")).Append("}");
    var table = Definitions("ContentTable", "tblPr").ToArray();
    foreach (string side in new[] { "top", "right", "bottom", "left" })
    {
      string padding = table.Select(p => (string)p.Element(w + "tblCellMar")?.Element(w + side)?.Attribute(w + "w")).FirstOrDefault(v => v != null);
      double points = Number(padding, 80) / 20;
      // Bare cell text is a Normal paragraph in Word as well. Account for its
      // space-after once; explicit final paragraphs and lists must not double it.
      double after = Number(Attribute("Normal", "pPr", "spacing", "after"), 0) / 20;
      css.Append($"td,th{{padding-{side}:{N(points + (side == "bottom" ? after : 0))}pt;}}");
      if (side == "bottom") css.Append($"td:has(>ul:last-child),th:has(>ul:last-child),td:has(>ol:last-child),th:has(>ol:last-child){{padding-bottom:{N(points)}pt;}}");
      var border = table.Select(p => p.Element(w + "tblBorders")?.Element(w + side)).FirstOrDefault(b => b != null);
      string color = (string)border?.Attribute(w + "color") ?? "000000";
      if (!Regex.IsMatch(color, "^[0-9A-Fa-f]{6}$")) color = "000000";
      css.Append($"td,th{{border-{side}:").Append((string)border?.Attribute(w + "val") is "nil" or "none" ? "none" :
        $"{N(Number((string)border?.Attribute(w + "sz"), 6) / 8)}pt solid #{color}").Append(";}");
    }
    css.Append("td>p:last-child,th>p:last-child{margin-bottom:0;}li>p{margin-top:0;margin-bottom:0;}");
    return css.ToString();
  }
}
