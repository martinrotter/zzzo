using System.IO;
using System.Text.Json;

namespace ZZZO;

/// <summary>Jednoduché lokální nastavení vzhledu aplikace.</summary>
internal sealed class NastaveniAplikace
{
  public double? OknoVlevo { get; set; }
  public double? OknoNahore { get; set; }
  public double? SirkaOkna { get; set; }
  public double? VyskaOkna { get; set; }
  public bool OknoMaximalizovane { get; set; }
  public double? VyskaSeznamuUsneseni { get; set; }
  public double? SirkaPaneluHlasovani { get; set; }

  public static NastaveniAplikace Nacist(string soubor)
  {
    try
    {
      if (!File.Exists(soubor)) return new NastaveniAplikace();
      return JsonSerializer.Deserialize<NastaveniAplikace>(File.ReadAllText(soubor), MoznostiJson)
        ?? new NastaveniAplikace();
    }
    catch
    {
      // Nastavení nesmí zabránit spuštění aplikace.
      return new NastaveniAplikace();
    }
  }

  public static void Ulozit(string soubor, NastaveniAplikace nastaveni)
  {
    try
    {
      string adresar = Path.GetDirectoryName(soubor)!;
      Directory.CreateDirectory(adresar);
      string docasnySoubor = soubor + ".tmp";
      File.WriteAllText(docasnySoubor, JsonSerializer.Serialize(nastaveni, MoznostiJson));
      File.Move(docasnySoubor, soubor, true);
    }
    catch
    {
      // Selhání pomocného nastavení nesmí zablokovat ukončení aplikace.
    }
  }

  private static readonly JsonSerializerOptions MoznostiJson = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };
}
