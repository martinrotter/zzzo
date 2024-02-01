using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class HlasovaniZastupitele
{
  #region Enumy

  public enum VolbaHlasovani
  {
    [Description("Pro")]
    Pro,

    [Description("Proti")]
    Proti,

    [Description("Zdržuje se")]
    ZdrzujeSe
  }

  #endregion

  #region Vlastnosti

  [JsonIgnore]
  public string JmenoPrijmeniZastupitele
  {
    get => $"{Zastupitel.Jmeno} {Zastupitel.Prijmeni}";
  }

  public string Poznamka
  {
    get
    {
      if (!Zastupitel.JePritomen)
      {
        return "nepřítomen";
      }
      else
      {
        return "-";
      }
    }
  }

  public VolbaHlasovani Volba
  {
    get;
    set;
  } = VolbaHlasovani.ZdrzujeSe;

  [JsonIgnore]
  public List<VolbaHlasovani> Volby
  {
    get => new List<VolbaHlasovani>
    {
      VolbaHlasovani.Pro,
      VolbaHlasovani.Proti,
      VolbaHlasovani.ZdrzujeSe
    };
  }

  [JsonProperty("Zastupitel", IsReference = true)]
  public Zastupitel Zastupitel
  {
    get;
    set;
  }

  #endregion
}