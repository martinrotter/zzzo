using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;

namespace ZZZO.Common;

public static class HtmlObsah
{
  private static readonly HashSet<string> Povolene = new HashSet<string> { "p", "div", "span", "br", "strong", "b", "em", "i", "u", "s", "sub", "sup", "ul", "ol", "li", "table", "thead", "tbody", "tfoot", "tr", "td", "th", "caption", "colgroup", "col", "a", "img", "blockquote", "h1", "h2", "h3", "h4", "hr" };

  public static string Zakodovat(string text) => WebUtility.HtmlEncode(text ?? "");
  public static string ProstyText(string html)
  {
    var dokument = new HtmlDocument();
    dokument.LoadHtml(html ?? "");
    foreach (var uzel in dokument.DocumentNode.SelectNodes("//script|//style")?.ToArray() ?? Array.Empty<HtmlNode>()) uzel.Remove();
    return Regex.Replace(HtmlEntity.DeEntitize(dokument.DocumentNode.InnerText), @"\s+", " ").Trim();
  }

  public static void Vlozit(XmlElement cil, string html)
  {
    var dokument = new HtmlDocument();
    dokument.LoadHtml(html ?? "");
    foreach (var uzel in dokument.DocumentNode.ChildNodes) VlozitUzel(cil, uzel);
  }

  private static void VlozitUzel(XmlElement cil, HtmlNode uzel)
  {
    if (uzel.NodeType == HtmlNodeType.Text)
    {
      cil.AppendChild(cil.OwnerDocument.CreateTextNode(HtmlEntity.DeEntitize(((HtmlTextNode)uzel).Text)));
      return;
    }
    if (uzel.NodeType != HtmlNodeType.Element || uzel.Name is "script" or "style" or "iframe" or "object" or "embed") return;
    if (!Povolene.Contains(uzel.Name))
    {
      foreach (var potomek in uzel.ChildNodes) VlozitUzel(cil, potomek);
      return;
    }
    var element = cil.OwnerDocument.CreateElement(uzel.Name);
    foreach (var atribut in uzel.Attributes)
    {
      if (atribut.Name is not ("style" or "href" or "src" or "alt" or "title" or "width" or "height" or "colspan" or "rowspan" or "start" or "type")) continue;
      var hodnota = HtmlEntity.DeEntitize(atribut.Value);
      if (atribut.Name is "href" or "src")
      {
        if (!(hodnota.StartsWith("#") || Uri.TryCreate(hodnota, UriKind.Absolute, out var uri) &&
          (uri.Scheme is "http" or "https" or "mailto" || atribut.Name == "src" && hodnota.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)))) continue;
      }
      if (atribut.Name == "style" && Regex.IsMatch(hodnota, @"url\s*\(|expression\s*\(|@import", RegexOptions.IgnoreCase)) continue;
      element.SetAttribute(atribut.Name, hodnota);
    }
    cil.AppendChild(element);
    foreach (var potomek in uzel.ChildNodes) VlozitUzel(element, potomek);
  }
}
