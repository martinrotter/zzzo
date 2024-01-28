using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class Zasedani
{
  public DateTime DatumCas { get; set; } = DateTime.Now;

  public int PocetHostu { get; set; }

  public int Poradi { get; set; } = 1;

  public string NazevObce { get; set; }

  public Adresa AdresaKonani { get; set; }

  public ObservableCollection<Zastupitel> Zastupitele { get; set; }

  [JsonIgnore]
  public bool Spinave { get; set; }

  public void SaveToFile(string file)
  {
    var json = JsonConvert.SerializeObject(this, Formatting.Indented);

    File.WriteAllText(file, json, Encoding.UTF8);
  }

  public static Zasedani LoadFromFile(string file)
  {
    return JsonConvert.DeserializeObject<Zasedani>(File.ReadAllText(file, Encoding.UTF8));
  }
}