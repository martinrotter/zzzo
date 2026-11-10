using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using static ZZZO.Common.Generators.DokumentDocx;

namespace ZZZO.Common.Generators;

/// <summary>Converts editor fragments to native paragraphs, runs, lists, tables and drawings.</summary>
internal sealed class ObsahDocx
{
  private readonly DokumentDocx _document;
  private readonly Dictionary<string, byte[]> _images = new();
  private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };
  internal ObsahDocx(DokumentDocx document) => _document = document;
  private static readonly HashSet<string> Blocks = new() { "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "blockquote", "ul", "ol", "table", "hr" };

  internal List<XElement> Convert(string html, string style = "Normal", string prefix = "")
  {
    var doc = new HtmlDocument(); doc.LoadHtml(html ?? "");
    var output = ConvertBlocks(doc.DocumentNode.ChildNodes, style, 0);
    if (prefix.Length > 0)
    {
      if (output.FirstOrDefault()?.Name == W + "p") output[0].Element(W + "pPr").AddAfterSelf(Run(prefix));
      else output.Insert(0, P(prefix, style));
    }
    return output;
  }

  private List<XElement> ConvertBlocks(IEnumerable<HtmlNode> nodes, string style, int depth, int? availableWidth = null)
  {
    var result = new List<XElement>();
    var inline = new List<XElement>();
    void Flush() { if (inline.Count > 0) { result.Add(Paragraph(inline, style)); inline = new(); } }
    foreach (var node in nodes)
    {
      if (node.NodeType == HtmlNodeType.Comment || node.Name is "script" or "style" or "iframe" or "object" or "embed") continue;
      if (node.NodeType == HtmlNodeType.Text && string.IsNullOrWhiteSpace(HtmlEntity.DeEntitize(node.InnerText)) && inline.Count == 0) continue;
      if (!Blocks.Contains(node.Name)) { inline.AddRange(ConvertInline(node, new())); continue; }
      Flush();
      if (node.Name == "table") { result.Add(ConvertTable(node, availableWidth ?? _document.ContentWidth)); continue; }
      if (node.Name is "ul" or "ol") { result.AddRange(ConvertList(node, style, depth, availableWidth)); continue; }
      if (node.Name == "hr")
      {
        var line = P(""); line.Element(W + "pPr").Add(new XElement(W + "pBdr", new XElement(W + "bottom", new XAttribute(W + "val", "single"), new XAttribute(W + "sz", 6))));
        result.Add(line); continue;
      }
      string paragraphStyle = node.Name switch { "h1" or "h2" => "Heading1", "h3" or "h4" or "h5" or "h6" => "Heading2", _ => style };
      var children = ConvertBlocks(node.ChildNodes, paragraphStyle, depth, availableWidth);
      if (children.Count == 0) children.Add(P("", paragraphStyle));
      string align = Css(node, "text-align");
      foreach (var p in children.Where(e => e.Name == W + "p"))
      {
        if (p.Descendants(W + "drawing").Any()) p.Element(W + "pPr").Add(new XElement(W + "spacing", new XAttribute(W + "line", 242), new XAttribute(W + "lineRule", "atLeast")));
        if (align is "left" or "center" or "right" or "justify") p.Element(W + "pPr").Add(Val("jc", align == "justify" ? "both" : align));
        if (node.Name == "blockquote") p.Element(W + "pPr").Add(new XElement(W + "ind", new XAttribute(W + "left", 480), new XAttribute(W + "right", 480)));
      }
      result.AddRange(children);
    }
    Flush(); return result;
  }

  private List<XElement> ConvertList(HtmlNode list, string style, int depth, int? availableWidth)
  {
    var result = new List<XElement>();
    int index = int.TryParse(list.GetAttributeValue("start", "1"), out int start) ? start : 1;
    int numbering = _document.Numbering(list.Name == "ol", list.GetAttributeValue("type", "1"), index);
    foreach (var li in list.Elements("li"))
    {
      bool first = true;
      var pending = new List<HtmlNode>();
      void Flush()
      {
        if (pending.Count == 0 && !first) return;
        var children = ConvertBlocks(pending, style, depth, availableWidth); pending.Clear();
        if (first)
        {
          if (children.FirstOrDefault()?.Name != W + "p") children.Insert(0, P("", style));
          children[0].Element(W + "pPr").Add(new XElement(W + "numPr", Val("ilvl", Math.Min(depth, 8).ToString()), Val("numId", numbering.ToString())));
          first = false;
        }
        foreach (var p in children.Where(e => e.Name == W + "p"))
        {
          p.Element(W + "pPr").Add(new XElement(W + "ind", new XAttribute(W + "left", 360 * (depth + 1)), new XAttribute(W + "hanging", 180)));
          p.Element(W + "pPr").Add(new XElement(W + "spacing", new XAttribute(W + "after", 0)));
        }
        result.AddRange(children);
      }
      foreach (var node in li.ChildNodes)
      {
        if (node.Name is "ul" or "ol") { Flush(); result.AddRange(ConvertList(node, style, depth + 1, availableWidth)); }
        else pending.Add(node);
      }
      Flush();
    }
    return result;
  }

  private IEnumerable<XElement> ConvertInline(HtmlNode node, List<XElement> properties)
  {
    if (node.NodeType == HtmlNodeType.Text)
    {
      string text = Regex.Replace(HtmlEntity.DeEntitize(((HtmlTextNode)node).Text), @"[\t\r\n ]+", " ");
      if (text.Length > 0) yield return Run(text, properties.Select(p => new XElement(p)).ToArray());
      yield break;
    }
    if (node.NodeType != HtmlNodeType.Element || node.Name is "script" or "style" or "iframe" or "object" or "embed") yield break;
    if (node.Name == "br") { yield return new XElement(W + "r", new XElement(W + "br")); yield break; }
    if (node.Name == "img")
    {
      string source = HtmlEntity.DeEntitize(node.GetAttributeValue("src", ""));
      if (!_images.TryGetValue(source, out byte[] image))
      {
        if (source.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && source.Contains(";base64,")) image = System.Convert.FromBase64String(source[(source.IndexOf(',') + 1)..]);
        else if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") image = Client.GetByteArrayAsync(uri).GetAwaiter().GetResult();
        else throw new InvalidDataException("Obrázek v textu nemá podporovaný zdroj. Vložte jej jako datový obrázek nebo odkaz HTTPS.");
        _images[source] = image;
      }
      yield return _document.Image(image, 18.1, Dimension(node, "width"), Dimension(node, "height")); yield break;
    }
    var format = properties.Select(p => new XElement(p)).ToList();
    void Set(string name, XElement value) { format.RemoveAll(p => p.Name == W + name); format.Add(value); }
    if (node.Name is "b" or "strong" || Css(node, "font-weight") is "bold" or "700") Set("b", new XElement(W + "b"));
    if (node.Name is "em" or "i" || Css(node, "font-style") == "italic") Set("i", new XElement(W + "i"));
    if (node.Name == "u" || Css(node, "text-decoration").Contains("underline")) Set("u", Val("u", "single"));
    if (node.Name is "s" or "del" || Css(node, "text-decoration").Contains("line-through")) Set("strike", new XElement(W + "strike"));
    if (node.Name is "sub" or "sup") Set("vertAlign", Val("vertAlign", node.Name == "sub" ? "subscript" : "superscript"));
    string color = Color(Css(node, "color"));
    if (color != null) Set("color", Val("color", color));
    string background = Color(Css(node, "background-color"));
    if (background != null) Set("shd", new XElement(W + "shd", new XAttribute(W + "val", "clear"), new XAttribute(W + "fill", background)));
    var fontSize = Regex.Match(Css(node, "font-size"), @"^([0-9.]+)(pt|px)$");
    if (fontSize.Success && double.TryParse(fontSize.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double size))
      Set("sz", Val("sz", Math.Clamp((int)Math.Round(size * (fontSize.Groups[2].Value == "px" ? 1.5 : 2)), 2, 400).ToString(CultureInfo.InvariantCulture)));
    var content = node.ChildNodes.SelectMany(n => ConvertInline(n, format)).ToList();
    if (node.Name == "a" && Uri.TryCreate(HtmlEntity.DeEntitize(node.GetAttributeValue("href", "")), UriKind.Absolute, out var link) && link.Scheme is "https" or "http" or "mailto")
      yield return new XElement(W + "hyperlink", new XAttribute(R + "id", _document.Hyperlink(link)), content);
    else foreach (var element in content) yield return element;
  }

  private static string Css(HtmlNode node, string property)
  {
    var match = Regex.Match(node.GetAttributeValue("style", ""), @"(?:^|;)\s*" + Regex.Escape(property) + @"\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value.Trim().ToLowerInvariant() : "";
  }
  private static string Color(string value)
  {
    if (Regex.IsMatch(value, "^#[0-9a-f]{6}$")) return value[1..].ToUpperInvariant();
    if (Regex.IsMatch(value, "^#[0-9a-f]{3}$")) return string.Concat(value[1..].Select(c => new string(c, 2))).ToUpperInvariant();
    var rgb = Regex.Match(value, @"^rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$");
    if (rgb.Success) return string.Concat(Enumerable.Range(1, 3).Select(i => Math.Clamp(int.Parse(rgb.Groups[i].Value), 0, 255).ToString("X2")));
    return value switch { "black" => "000000", "white" => "FFFFFF", "red" => "FF0000", "green" => "008000", "blue" => "0000FF", "yellow" => "FFFF00", _ => null };
  }
  private static double? Dimension(HtmlNode node, string name)
  {
    string value = Css(node, name); if (value.Length == 0) value = node.GetAttributeValue(name, "");
    return double.TryParse(value.Replace("px", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double size) && size > 0 ? size : null;
  }

  private XElement ConvertTable(HtmlNode source, int availableWidth)
  {
    var layout = new RozvrzeniHtmlTabulky(source, availableWidth);
    var table = new XElement(W + "tbl", _document.TableProperties("ContentTable", layout.Sirka),
      new XElement(W + "tblGrid", layout.Sirky.Select(w => new XElement(W + "gridCol", new XAttribute(W + "w", w)))));
    foreach (var row in layout.Radky)
    {
      var tr = new XElement(W + "tr");
      if (row.Zdroj?.ParentNode?.Name == "thead") tr.Add(new XElement(W + "trPr", new XElement(W + "tblHeader")));
      foreach (var cell in row.Bunky)
      {
        int width = layout.SirkaBunky(cell);
        var properties = new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", width), new XAttribute(W + "type", "dxa")));
        if (cell.PocetSloupcu > 1) properties.Add(Val("gridSpan", cell.PocetSloupcu.ToString(CultureInfo.InvariantCulture)));
        var tc = new XElement(W + "tc", properties);
        if (cell.Pokracovani) { properties.Add(new XElement(W + "vMerge")); tc.Add(P("")); }
        else if (cell.Zdroj is HtmlNode node)
        {
          if (Span(node, "rowspan") > 1) properties.Add(Val("vMerge", "restart"));
          var blocks = ConvertBlocks(node.ChildNodes, "Normal", 0, Math.Max(1, width - 160));
          if (blocks.Count == 0 || blocks[^1].Name != W + "p") blocks.Add(P(""));
          string align = Css(node, "text-align");
          if (align is "left" or "center" or "right" or "justify") foreach (var p in blocks.Where(b => b.Name == W + "p"))
            if (p.Element(W + "pPr").Element(W + "jc") == null) p.Element(W + "pPr").Add(Val("jc", align == "justify" ? "both" : align));
          if (node.Name == "th") foreach (var run in blocks.Descendants(W + "r"))
          { if (run.Element(W + "rPr") == null) run.AddFirst(new XElement(W + "rPr")); if (run.Element(W + "rPr").Element(W + "b") == null) run.Element(W + "rPr").Add(new XElement(W + "b")); }
          tc.Add(blocks);
        }
        else tc.Add(P(""));
        tr.Add(tc);
      }
      table.Add(tr);
    }
    return table;
  }
  private static int Span(HtmlNode node, string name) => Math.Clamp(node.GetAttributeValue(name, 1), 1, 100);
}
