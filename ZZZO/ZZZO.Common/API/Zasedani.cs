using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class Zasedani
{
  #region Vlastnosti

  public Adresa AdresaKonani
  {
    get;
    set;
  } = new Adresa();

  public DateTime DatumCas
  {
    get;
    set;
  } = DateTime.Now;

  public string NazevObce
  {
    get;
    set;
  }

  public int PocetHostu
  {
    get;
    set;
  }

  public int Poradi
  {
    get;
    set;
  } = 1;

  public Program Program
  {
    get;
    set;
  } = new Program();

  [JsonIgnore]
  public bool Spinave
  {
    get;
    set;
  }

  public ObservableCollection<Zastupitel> Zastupitele
  {
    get;
    set;
  } = new ObservableCollection<Zastupitel>();

  #endregion

  #region Metody

  public static Zasedani LoadFromFile(string file)
  {
    Zasedani zas = JsonConvert.DeserializeObject<Zasedani>(File.ReadAllText(file, Encoding.UTF8));

    zas.InitializeTree();

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

  public void InitializeTree()
  {
    if (Zastupitele == null)
    {
      return;
    }

    foreach (Zastupitel zastupitel in Zastupitele)
    {
      zastupitel.Zasedani = this;
    }
  }

  public void RemoveZastupitel(Zastupitel zast)
  {
    Zastupitele.Remove(zast);

    foreach (BodProgramu bodProgramu in Program.BodyProgramu)
    {
      foreach (Usneseni usneseni in bodProgramu.Usneseni)
      {
        // Odebereme zastupitele.
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
    string json = JsonConvert.SerializeObject(this, Formatting.Indented);

    File.WriteAllText(file, json, Encoding.UTF8);
  }

  #endregion
}