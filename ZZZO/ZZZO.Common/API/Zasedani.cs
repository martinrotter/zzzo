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
  private DateTime _datumCasKonani = DateTime.Now;
  private BitmapImage _logoObce;
  private string _nazevObce;
  private int _pocetHostu;
  private int _poradi = 1;
  private Program _program = new Program();
  private string _vystupniSoubor;
  private ObservableCollection<Zastupitel> _zastupitele = new ObservableCollection<Zastupitel>();
  private string _htmlStyle;

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

  [JsonIgnore]
  public DateTime CasKonani
  {
    get => DatumCasKonani;
    set => DatumCasKonani = new DateTime(DatumCasKonani.Year, DatumCasKonani.Month, DatumCasKonani.Day, value.Hour, value.Minute, value.Second);
  }

  public DateTime DatumCasKonani
  {
    get => _datumCasKonani;
    set
    {
      if (value.Equals(_datumCasKonani))
      {
        return;
      }

      _datumCasKonani = value;
      OnPropertyChanged();
    }
  }

  [JsonIgnore]
  public DateTime DatumKonani
  {
    get => DatumCasKonani;
    set => DatumCasKonani = new DateTime(value.Year, value.Month, value.Day, DatumCasKonani.Hour, DatumCasKonani.Minute, DatumCasKonani.Second);
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
      if (value == null || value.Length == 0)
      {
        LogoObce = null;
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

  /// <summary>
  /// Výstupní soubor bez přípony.
  /// </summary>
  public string VystupniSoubor
  {
    get
    {
      return _vystupniSoubor ??= Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        $"zápis-{DatumKonani:yyyy-MM}-zo");
    }

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

  public string HtmlStyle
  {
    get => _htmlStyle;
    set
    {
      if (value == _htmlStyle)
      {
        return;
      }

      _htmlStyle = value;
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
    byte[] json = ToJson();
    File.WriteAllBytes(file, json);
  }

  public byte[] ToJson()
  {
    string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
    {
      DateTimeZoneHandling = DateTimeZoneHandling.Local,
      DateFormatString = "yyyy-MM-ddThh:mm",
      Formatting = Formatting.Indented
    });

    return Encoding.UTF8.GetBytes(json);
  }

  #endregion

  public static Zasedani GenerateSample()
  {
    Zasedani zas = new Zasedani();

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "Novák",
      Jmeno = "Petr",
      JeOverovatel = false,
      JeStarosta = true,
      JeZapisovatel = false,
      JePritomen = true,
      JeRidici = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "Černý",
      Jmeno = "Pavel",
      JeOverovatel = true,
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "Bílý",
      Jmeno = "Zdeněk",
      JeOverovatel = true,
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "Fialový",
      Jmeno = "Alex",
      JePritomen = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "Zelený",
      Jmeno = "Miloš",
      JePritomen = true,
      JeZapisovatel = true
    });

    zas.AddZastupitel(new Zastupitel
    {
      Prijmeni = "Růžový",
      Jmeno = "Petr",
      JePritomen = false
    });

    zas.NazevObce = "Praha";
    zas.DatumCasKonani = DateTime.Now;

    zas.AdresaKonani = new Adresa
    {
      CisloPopisneOrientacni = "12",
      Obec = "Krakatit",
      Psc = "779 00",
      Ulice = "Čtenářská",
      PopisMista = "zasedací místnosti"
    };

    var schvaleniProgramu = new BodProgramu
    {
      SchvalovaniProgramu = true,
      Nadpis = "Schvalování programu"
    };

    zas.Program.BodyProgramu.Add(schvaleniProgramu);

    zas.AddUsneseni(schvaleniProgramu, new Usneseni
    {
      Text = "Hlasování o programu"
    });
    
    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "První",
      Text = "Detailní popis bodu programu."
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Druhý",
      JePodbod = true
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Třetí",
      JePodbod = true
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Čtvrtý",
      JeDoplneny = true
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Různé"
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Diskuse"
    });

    zas.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Závěr"
    });

    return zas;
  }
}