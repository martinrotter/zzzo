using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class Zasedani : ObservableObject
{
  #region Proměnné

  private Adresa _adresaKonani = new Adresa();
  private DateTime _datumCas = DateTime.Now;
  private BitmapImage _logoObce;
  private string _nazevObce;
  private int _pocetHostu;
  private int _poradi = 1;
  private Program _program = new Program();
  private bool _spinave;
  private string _vystupniSoubor;
  private ObservableCollection<Zastupitel> _zastupitele = new ObservableCollection<Zastupitel>();

  #endregion

  #region Vlastnosti

  public Adresa AdresaKonani
  {
    get => _adresaKonani;
    set
    {
      if (Equals(value, _adresaKonani))
      {
        return;
      }

      _adresaKonani = value;
      OnPropertyChanged();
    }
  }

  public DateTime CasKonani
  {
    get => DatumCas;
    set => DatumCas = new DateTime(DatumCas.Year, DatumCas.Month, DatumCas.Day, value.Hour, value.Minute, value.Second);
  }

  public DateTime DatumCas
  {
    get => _datumCas;
    set
    {
      if (value.Equals(_datumCas))
      {
        return;
      }

      _datumCas = value;
      OnPropertyChanged();
    }
  }

  public DateTime DatumKonani
  {
    get => DatumCas;
    set => DatumCas = new DateTime(value.Year, value.Month, value.Day, DatumCas.Hour, DatumCas.Minute, DatumCas.Second);
  }

  [JsonIgnore]
  public BitmapImage LogoObce
  {
    get => _logoObce;
    set
    {
      if (Equals(value, _logoObce))
      {
        return;
      }

      _logoObce = value;
      OnPropertyChanged();
    }
  }

  [JsonProperty("LogoObce")]
  public byte[] LogoObceData
  {
    get
    {
      if (LogoObce == null)
      {
        return null;
      }

      byte[] data;
      PngBitmapEncoder encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(LogoObce));

      using (MemoryStream ms = new MemoryStream())
      {
        encoder.Save(ms);
        return ms.ToArray();
      }
    }

    set
    {
      if (value == null)
      {
        return;
      }

      using (MemoryStream stream = new MemoryStream(value))
      {
        BitmapImage bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = stream;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        LogoObce = bitmap;
      }
    }
  }

  public string NazevObce
  {
    get => _nazevObce;
    set
    {
      if (value == _nazevObce)
      {
        return;
      }

      _nazevObce = value;
      OnPropertyChanged();
    }
  }

  public int PocetHostu
  {
    get => _pocetHostu;
    set
    {
      if (value == _pocetHostu)
      {
        return;
      }

      _pocetHostu = value;
      OnPropertyChanged();
    }
  }

  public int Poradi
  {
    get => _poradi;
    set
    {
      if (value == _poradi)
      {
        return;
      }

      _poradi = value;
      OnPropertyChanged();
    }
  }

  public Program Program
  {
    get => _program;
    set
    {
      if (Equals(value, _program))
      {
        return;
      }

      _program = value;
      OnPropertyChanged();
    }
  }

  [JsonIgnore]
  public bool Spinave
  {
    get => _spinave;
    set
    {
      if (value == _spinave)
      {
        return;
      }

      _spinave = value;
      OnPropertyChanged();
    }
  }

  /// <summary>
  /// Výstupní soubor bez přípony.
  /// </summary>
  public string VystupniSoubor
  {
    get => _vystupniSoubor;
    set
    {
      if (value == _vystupniSoubor)
      {
        return;
      }

      _vystupniSoubor = value;
      OnPropertyChanged();
    }
  }

  [JsonProperty("Zastupitele", ItemIsReference = true, Order = -2)]
  public ObservableCollection<Zastupitel> Zastupitele
  {
    get => _zastupitele;
    set
    {
      if (Equals(value, _zastupitele))
      {
        return;
      }

      _zastupitele = value;
      OnPropertyChanged();
    }
  }

  #endregion

  #region Metody

  public static Zasedani LoadFromFile(string file)
  {
    Zasedani zas = JsonConvert.DeserializeObject<Zasedani>(File.ReadAllText(file, Encoding.UTF8));
    return zas;
  }

  public void AddUsneseni(BodProgramu bodProgramu, Usneseni usneseni)
  {
    bodProgramu.Usneseni.Add(usneseni);

    foreach (Zastupitel zastupitel in Zastupitele)
    {
      // Přidáme nového zastupitele.
      usneseni.VolbyZastupitelu.Add(new HlasovaniZastupitele
      {
        Zastupitel = zastupitel
      });
    }
  }

  public void AddZastupitel(Zastupitel zastupitel)
  {
    Zastupitele.Add(zastupitel);

    foreach (BodProgramu bodProgramu in Program.BodyProgramu)
    {
      foreach (Usneseni usneseni in bodProgramu.Usneseni)
      {
        // Přidáme nového zastupitele.
        usneseni.VolbyZastupitelu.Add(new HlasovaniZastupitele
        {
          Zastupitel = zastupitel
        });
      }
    }
  }

  public void RemoveZastupitel(Zastupitel zast)
  {
    Zastupitele.Remove(zast);

    foreach (BodProgramu bodProgramu in Program.BodyProgramu)
    {
      foreach (Usneseni usneseni in bodProgramu.Usneseni)
      {
        // Odebereme zastupitele ze všech usnesení.
        foreach (HlasovaniZastupitele hlasovaniZastupitele in usneseni.VolbyZastupitelu)
        {
          if (ReferenceEquals(hlasovaniZastupitele.Zastupitel, zast))
          {
            usneseni.VolbyZastupitelu.Remove(hlasovaniZastupitele);
            break;
          }
        }
      }
    }
  }

  public void SaveToFile(string file)
  {
    string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
    {
      DateTimeZoneHandling = DateTimeZoneHandling.Local,
      DateFormatString = "yyyy-MM-ddThh:mm",
      Formatting = Formatting.Indented
    });

    File.WriteAllText(file, json, Encoding.UTF8);
  }

  #endregion
}