using System.IO;
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ZZZO.Common.Generators;

/// <summary>Edits native OOXML parts, retaining styles, fonts and sections from the template.</summary>
internal sealed class DokumentDocx : IDisposable
{
  internal static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
  internal static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
  private readonly MemoryStream _stream = new();
  private readonly WordprocessingDocument _package;
  private readonly MainDocumentPart _main;
  private readonly XDocument _document;
  private readonly Dictionary<string, XElement> _tableProperties;
  private readonly Dictionary<string, XElement> _styles;
  private uint _drawingId;
  internal XElement Body => _document.Root.Element(W + "body");
  internal int ContentWidth
  {
    get
    {
      var section = Body.Element(W + "sectPr");
      int page = (int?)section?.Element(W + "pgSz")?.Attribute(W + "w") ?? 11906;
      var margins = section?.Element(W + "pgMar");
      return Math.Max(1, page - ((int?)margins?.Attribute(W + "left") ?? 823) - ((int?)margins?.Attribute(W + "right") ?? 823));
    }
  }
  internal void SetTitle(string title) => _package.PackageProperties.Title = title;

  internal DokumentDocx(byte[] template)
  {
    _stream.Write(template);
    _stream.Position = 0;
    _package = WordprocessingDocument.Open(_stream, true);
    _main = _package.MainDocumentPart ?? throw new InvalidDataException("Šablona neobsahuje dokument Wordu.");
    _document = Read(_main);
    _drawingId = _document.Descendants().Where(e => e.Name.LocalName == "docPr").Select(e => (uint?)e.Attribute("id") ?? 0).DefaultIfEmpty().Max();
    var styles = Read(_main.StyleDefinitionsPart ?? throw new InvalidDataException("V šabloně chybí styly."));
    _styles = styles.Descendants(W + "style").ToDictionary(s => (string)s.Attribute(W + "styleId"));
    _tableProperties = styles.Descendants(W + "style").Where(s => s.Element(W + "tblPr") != null)
      .ToDictionary(s => (string)s.Attribute(W + "styleId"), s => s.Element(W + "tblPr"));
    foreach (string id in new[] { "Normal", "Title", "Heading1", "Heading2", "Logo", "Agenda", "Subagenda", "Resolution", "Vote", "VoteHeading", "VoteLine", "DecisionAccepted", "DecisionRejected", "SummaryTitle", "Signature", "VoteAccepted", "VoteRejected", "Signatures", "ContentTable", "DecisionBoxAccepted", "DecisionBoxRejected", "AfterAgenda", "TableEnd" })
      if (!styles.Descendants(W + "style").Any(s => (string)s.Attribute(W + "styleId") == id))
        throw new InvalidDataException($"V DOCX šabloně chybí styl {id}.");
    foreach (string tag in new[] { "Logo", "Title", "Opening", "Body", "Resolutions", "Signatures" })
      if (Find(tag).Count != 1) throw new InvalidDataException($"Šablona musí obsahovat právě jeden blok ZZZO.{tag}.");
    if (_main.FontTablePart == null || !_main.FontTablePart.FontParts.Any())
      throw new InvalidDataException("DOCX šablona neobsahuje vložené fonty.");
  }

  private static XDocument Read(OpenXmlPart part)
  {
    using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
    return XDocument.Load(stream);
  }

  private List<XElement> Find(string tag) => Body.Descendants(W + "sdt").Where(s =>
    (string)s.Element(W + "sdtPr")?.Element(W + "tag")?.Attribute(W + "val") == "ZZZO." + tag).ToList();

  internal void Replace(string tag, IEnumerable<XElement> content) => Find(tag).Single().ReplaceWith(content);

  internal static XElement Val(string tag, string value) => new(W + tag, new XAttribute(W + "val", value));
  internal static XElement Run(string text, params XElement[] properties) => new(W + "r",
    properties.Length == 0 ? null : new XElement(W + "rPr", properties),
    new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text));
  internal static XElement P(string text, string style = "Normal") => Paragraph(new[] { Run(text) }, style);
  internal static XElement Paragraph(IEnumerable<XElement> runs, string style = "Normal") => new(W + "p",
    new XElement(W + "pPr", Val("pStyle", style)), runs);

  internal XElement Table(string style, IEnumerable<XElement[]> rows, int[] widths)
  {
    var properties = TableProperties(style, widths.Sum());
    var table = new XElement(W + "tbl", properties,
      new XElement(W + "tblGrid", widths.Select(w => new XElement(W + "gridCol", new XAttribute(W + "w", w)))));
    foreach (var cells in rows)
    {
      var row = new XElement(W + "tr");
      for (int i = 0; i < cells.Length; i++)
      {
        var cellProperties = new XElement(W + "tcPr", new XElement(W + "tcW", new XAttribute(W + "w", widths[Math.Min(i, widths.Length - 1)]), new XAttribute(W + "type", "dxa")),
          _tableProperties.GetValueOrDefault(style)?.Element(W + "shd") is XElement shading ? new XElement(shading) : null);
        if (style.StartsWith("DecisionBox", StringComparison.Ordinal)) cellProperties.Add(new XElement(W + "noWrap"), Val("vAlign", "center"));
        row.Add(new XElement(W + "tc", cellProperties, cells[i].Name == "cell" ? cells[i].Elements() : new[] { cells[i] }));
      }
      table.Add(row);
    }
    return table;
  }

  internal int DecisionWidth(XElement paragraph, string tableStyle)
  {
    string style = (string)paragraph.Element(W + "pPr")?.Element(W + "pStyle")?.Attribute(W + "val") ?? "Normal";
    XElement Property(string name)
    {
      string current = style;
      var visited = new HashSet<string>();
      while (current != null && visited.Add(current) && _styles.TryGetValue(current, out var definition))
      {
        if (definition.Element(W + "rPr")?.Element(W + name) is XElement property) return property;
        current = (string)definition.Element(W + "basedOn")?.Attribute(W + "val");
      }
      return null;
    }
    var fonts = new Uri(Path.GetFullPath(Path.Combine(Constants.PathsAndFiles.AppDataFolder, "Fonts")) + Path.DirectorySeparatorChar);
    double width = 0;
    foreach (var run in paragraph.Elements(W + "r"))
    {
      string text = string.Concat(run.Elements(W + "t").Select(t => t.Value));
      var properties = run.Element(W + "rPr");
      bool Enabled(string name) => (properties?.Element(W + name) ?? Property(name)) is XElement property
        && (string)property.Attribute(W + "val") is not ("0" or "false" or "off");
      string family = (string)(properties?.Element(W + "rFonts") ?? Property("rFonts"))?.Attribute(W + "ascii") ?? "EB Garamond";
      double size = double.TryParse((string)(properties?.Element(W + "sz") ?? Property("sz"))?.Attribute(W + "val"), NumberStyles.Float, CultureInfo.InvariantCulture, out double halfPoints) ? halfPoints / 2 : 11;
      var typeface = new Typeface(new System.Windows.Media.FontFamily(fonts, "./#" + family),
        Enabled("i") ? FontStyles.Italic : FontStyles.Normal,
        Enabled("b") ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
      width += new FormattedText(text, CultureInfo.GetCultureInfo("cs-CZ"), FlowDirection.LeftToRight,
        typeface, size * 96 / 72, Brushes.Black, 1).WidthIncludingTrailingWhitespace * 15;
    }
    var margins = _tableProperties.GetValueOrDefault(tableStyle)?.Element(W + "tblCellMar");
    int Padding(string side) => (int?)margins?.Element(W + side)?.Attribute(W + "w") ?? 120;
    // An advisory auto width is interpreted differently by Word and LibreOffice for
    // nested tables. Measure the actual bundled fonts and leave 2 pt for shaping differences.
    return (int)Math.Ceiling(width) + Padding("left") + Padding("right") + 40;
  }

  internal XElement TableProperties(string style, int width) => new(W + "tblPr", Val("tblStyle", style),
    new XElement(W + "tblW", new XAttribute(W + "w", width), new XAttribute(W + "type", "dxa")),
    _tableProperties.GetValueOrDefault(style)?.Elements().Where(e => e.Name != W + "tblCellMar").Select(e => new XElement(e)),
    new XElement(W + "tblLayout", new XAttribute(W + "type", "fixed")),
    _tableProperties.GetValueOrDefault(style)?.Elements(W + "tblCellMar").Select(e => new XElement(e)));

  internal static XElement Symbol(string text, string color) => text.Contains('✓')
    ? Run(text, new XElement(W + "rFonts", new XAttribute(W + "ascii", "Noto Sans Symbols 2"), new XAttribute(W + "hAnsi", "Noto Sans Symbols 2")), Val("color", color))
    : Run(text, Val("color", color));

  internal string Hyperlink(Uri target) => _main.AddHyperlinkRelationship(target, true).Id;

  internal int Numbering(bool ordered, string type, int start)
  {
    var part = _main.NumberingDefinitionsPart ?? _main.AddNewPart<NumberingDefinitionsPart>();
    XDocument numbering;
    using (var input = part.GetStream()) numbering = input.Length == 0 ? new XDocument(new XElement(W + "numbering")) : XDocument.Load(input);
    int id = numbering.Root.Elements(W + "abstractNum").Select(e => (int)e.Attribute(W + "abstractNumId")).DefaultIfEmpty(-1).Max() + 1;
    int numId = numbering.Root.Elements(W + "num").Select(e => (int)e.Attribute(W + "numId")).DefaultIfEmpty(0).Max() + 1;
    string format = !ordered ? "bullet" : type switch { "a" => "lowerLetter", "A" => "upperLetter", "i" => "lowerRoman", "I" => "upperRoman", _ => "decimal" };
    var definition = new XElement(W + "abstractNum", new XAttribute(W + "abstractNumId", id), Val("multiLevelType", "multilevel"));
    for (int level = 0; level < 9; level++) definition.Add(new XElement(W + "lvl", new XAttribute(W + "ilvl", level),
      Val("start", start.ToString()), Val("numFmt", format), Val("lvlText", ordered ? "%" + (level + 1) + "." : "•"), Val("lvlJc", "left"),
      new XElement(W + "pPr", new XElement(W + "tabs", new XElement(W + "tab", new XAttribute(W + "val", "num"), new XAttribute(W + "pos", 360 * (level + 1)))),
        new XElement(W + "ind", new XAttribute(W + "left", 360 * (level + 1)), new XAttribute(W + "hanging", 180)))));
    var firstNum = numbering.Root.Element(W + "num");
    if (firstNum == null) numbering.Root.Add(definition); else firstNum.AddBeforeSelf(definition);
    numbering.Root.Add(new XElement(W + "num", new XAttribute(W + "numId", numId), Val("abstractNumId", id.ToString())));
    using var output = part.GetStream(FileMode.Create, FileAccess.Write); numbering.Save(output);
    return numId;
  }

  internal XElement Image(byte[] data, double maxWidthCm, double? requestedWidthPx = null, double? requestedHeightPx = null)
  {
    // Decode locally; images in generated files are always self-contained PNGs.
    using var input = new MemoryStream(data);
    var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
    var frame = decoder.Frames[0];
    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(frame);
    using var png = new MemoryStream(); encoder.Save(png);
    string filename = "image" + Guid.NewGuid().ToString("N") + ".png";
    var image = _main.AddImagePart(ImagePartType.Png);
    png.Position = 0; image.FeedData(png);
    string id = _main.GetIdOfPart(image);
    double ratio = requestedWidthPx.HasValue && requestedHeightPx.HasValue ? requestedHeightPx.Value / requestedWidthPx.Value : (double)frame.PixelHeight / frame.PixelWidth;
    double width = Math.Min(maxWidthCm / 2.54 * 914400, (requestedWidthPx ?? (requestedHeightPx.HasValue ? requestedHeightPx.Value / ratio : frame.PixelWidth)) * 9525);
    double height = width * ratio;
    if (height > 8500000) { width *= 8500000 / height; height = 8500000; }
    uint drawingId = ++_drawingId;
    XNamespace wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
    XNamespace pic = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    return new XElement(W + "r", new XElement(W + "drawing", new XElement(wp + "inline",
      new XElement(wp + "extent", new XAttribute("cx", (long)width), new XAttribute("cy", (long)height)),
      new XElement(wp + "docPr", new XAttribute("id", drawingId), new XAttribute("name", filename)),
      new XElement(a + "graphic", new XElement(a + "graphicData", new XAttribute("uri", pic.NamespaceName),
        new XElement(pic + "pic",
          new XElement(pic + "nvPicPr", new XElement(pic + "cNvPr", new XAttribute("id", 0), new XAttribute("name", filename)), new XElement(pic + "cNvPicPr")),
          new XElement(pic + "blipFill", new XElement(a + "blip", new XAttribute(R + "embed", id)), new XElement(a + "stretch", new XElement(a + "fillRect"))),
          new XElement(pic + "spPr", new XElement(a + "xfrm", new XElement(a + "off", new XAttribute("x", 0), new XAttribute("y", 0)),
            new XElement(a + "ext", new XAttribute("cx", (long)width), new XAttribute("cy", (long)height))),
            new XElement(a + "prstGeom", new XAttribute("prst", "rect"), new XElement(a + "avLst")))))))));
  }

  internal byte[] Save()
  {
    // The SDK orders properties according to the OOXML schema, including properties
    // composed by separate content converters (numbering, alignment, keep-with-next).
    foreach (var element in _document.Descendants().Where(e => e.Name == W + "pPr" || e.Name == W + "rPr").ToArray())
    {
      DocumentFormat.OpenXml.OpenXmlCompositeElement properties = element.Name == W + "pPr"
        ? new ParagraphProperties(element.ToString()) : new RunProperties(element.ToString());
      var children = properties.ChildElements.ToArray(); properties.RemoveAllChildren();
      foreach (var child in children) properties.AddChild(child, true);
      element.ReplaceWith(XElement.Parse(properties.OuterXml));
    }
    using (var output = _main.GetStream(FileMode.Create, FileAccess.Write)) _document.Save(output);
    _package.Dispose();
    return _stream.ToArray();
  }

  public void Dispose() { _package.Dispose(); _stream.Dispose(); }
}
