using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class HlasovaniZastupitele : ObservableObject
{
  private VolbaHlasovani _volba = VolbaHlasovani.ZdrzujeSe;
  private Zastupitel _zastupitel;

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
    get => _volba;
    set
    {
      if (value == _volba)
      {
        return;
      }

      _volba = value;
      OnPropertyChanged();
    }
  }

  [JsonIgnore]
  public List<VolbaHlasovani> Volby
  {
    get => Enum.GetValues<VolbaHlasovani>().ToList();
  }

  [JsonProperty("Zastupitel", IsReference = true)]
  public Zastupitel Zastupitel
  {
    get => _zastupitel;
    set
    {
      if (Equals(value, _zastupitel))
      {
        return;
      }

      _zastupitel = value;

      _zastupitel.PropertyChanged += (sender, args) =>
      {
        OnPropertyChanged(nameof(JmenoPrijmeniZastupitele));
        OnPropertyChanged(nameof(Poznamka));
      };

      OnPropertyChanged();
      OnPropertyChanged(nameof(JmenoPrijmeniZastupitele));
      OnPropertyChanged(nameof(Poznamka));
    }
  }

  #endregion
}