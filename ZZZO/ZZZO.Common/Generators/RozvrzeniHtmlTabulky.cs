using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ZZZO.Common.Generators;

/// <summary>Resolves the HTML grid before assigning Word widths, including merged cells.</summary>
internal sealed class RozvrzeniHtmlTabulky
{
  internal sealed record Bunka(HtmlNode Zdroj, int Sloupec, int PocetSloupcu, bool Pokracovani = false);
  internal List<(HtmlNode Zdroj, List<Bunka> Bunky)> Radky { get; } = new();
  internal int[] Sirky { get; }
  internal int Sirka => Sirky.Sum();
  internal int SirkaBunky(Bunka cell) => Sirky.Skip(cell.Sloupec).Take(cell.PocetSloupcu).Sum();

  internal RozvrzeniHtmlTabulky(HtmlNode table, int availableWidth)
  {
    var merged = new Dictionary<int, (int Remaining, int Span)>();
    int columns = 1;
    foreach (var row in table.SelectNodes("./tr|./thead/tr|./tbody/tr|./tfoot/tr")?.ToList() ?? new())
    {
      var cells = new Queue<HtmlNode>(row.ChildNodes.Where(n => n.Name is "td" or "th"));
      var grid = new List<Bunka>();
      int col = 0;
      while (cells.Count > 0 || merged.Keys.Any(k => k >= col))
      {
        if (merged.TryGetValue(col, out var merge))
        {
          grid.Add(new Bunka(null, col, merge.Span, true));
          if (merge.Remaining == 1) merged.Remove(col);
          else merged[col] = (merge.Remaining - 1, merge.Span);
          col += merge.Span;
        }
        else if (cells.TryDequeue(out var cell))
        {
          int span = Span(cell, "colspan");
          // Do not let a malformed colspan overwrite a continuing vertical merge.
          int nextMerge = merged.Keys.Where(k => k > col).DefaultIfEmpty(col + span).Min();
          span = Math.Min(span, nextMerge - col);
          grid.Add(new Bunka(cell, col, span));
          int height = Span(cell, "rowspan");
          if (height > 1) merged[col] = (height - 1, span);
          col += span;
        }
        else { grid.Add(new Bunka(null, col, 1)); col++; }
        if (col > 63) throw new InvalidDataException("Tabulka pro DOCX může mít nejvýše 63 sloupců.");
      }
      columns = Math.Max(columns, col);
      Radky.Add((row, grid));
    }
    if (Radky.Count == 0) Radky.Add((null, new List<Bunka>()));
    var declaredColumns = (table.SelectNodes("./colgroup/col|./col")?.ToList() ?? new())
      .SelectMany(c => Enumerable.Repeat(c, Span(c, "span"))).ToList();
    columns = Math.Max(columns, declaredColumns.Count);
    if (columns > 63) throw new InvalidDataException("Tabulka pro DOCX může mít nejvýše 63 sloupců.");
    foreach (var row in Radky)
    {
      int end = row.Bunky.Select(c => c.Sloupec + c.PocetSloupcu).DefaultIfEmpty(0).Max();
      for (int col = end; col < columns; col++) row.Bunky.Add(new Bunka(null, col, 1));
    }

    double nominalWidth = Width(table, availableWidth) ?? availableWidth;
    int targetWidth = (int)Math.Clamp(Math.Round(nominalWidth), columns, Math.Max(columns, availableWidth));
    var widths = new double?[columns];
    for (int col = 0; col < declaredColumns.Count; col++) widths[col] = Width(declaredColumns[col], nominalWidth);
    // Colgroups are authoritative. Unmerged cell widths provide the next best
    // constraints; merged-cell widths fill only columns that remain unspecified.
    foreach (var cell in Radky.SelectMany(r => r.Bunky).Where(c => c.Zdroj != null).OrderBy(c => c.PocetSloupcu))
    {
      double? declared = Width(cell.Zdroj, nominalWidth);
      if (declared == null) continue;
      var indexes = Enumerable.Range(cell.Sloupec, cell.PocetSloupcu).ToArray();
      var unknown = indexes.Where(i => widths[i] == null).ToArray();
      if (unknown.Length == 0) continue;
      double remaining = declared.Value - indexes.Sum(i => widths[i] ?? 0);
      if (remaining > 0) foreach (int i in unknown) widths[i] = remaining / unknown.Length;
    }
    int missing = widths.Count(w => w == null);
    double known = widths.Sum(w => w ?? 0);
    double fallback = missing == columns ? nominalWidth / columns
      : known < nominalWidth && missing > 0 ? (nominalWidth - known) / missing : known / (columns - missing);
    double[] weights = widths.Select(w => w ?? fallback).ToArray();
    double total = weights.Sum();
    double[] exact = weights.Select(w => w / total * (targetWidth - columns)).ToArray();
    Sirky = exact.Select(w => (int)Math.Floor(w) + 1).ToArray();
    foreach (int i in Enumerable.Range(0, columns).OrderByDescending(i => exact[i] - Math.Floor(exact[i])).Take(targetWidth - Sirky.Sum())) Sirky[i]++;
  }

  private static int Span(HtmlNode node, string name) => Math.Clamp(node.GetAttributeValue(name, 1), 1, 100);
  private static double? Width(HtmlNode node, double basis)
  {
    string css = Regex.Match(node.GetAttributeValue("style", ""), @"(?:^|;)\s*width\s*:\s*([^;]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
    string value = css.Length == 0 ? node.GetAttributeValue("width", "") : css;
    var match = Regex.Match(value.Trim(), @"^([0-9]+(?:\.[0-9]+)?)(%|px|pt|cm|mm|in)?$", RegexOptions.IgnoreCase);
    if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
      || !double.IsFinite(number) || number <= 0 || number > 1_000_000) return null;
    return Math.Min(100_000_000, number * (match.Groups[2].Value.ToLowerInvariant() switch
    { "%" => basis / 100, "pt" => 20, "cm" => 1440 / 2.54, "mm" => 1440 / 25.4, "in" => 1440, _ => 15 }));
  }
}
