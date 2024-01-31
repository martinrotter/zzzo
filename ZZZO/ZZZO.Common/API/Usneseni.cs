using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class Usneseni
{
  #region Vlastnosti

  [JsonIgnore]
  public bool JeSchvaleno
  {
    get => VolbyZastupitelu.Count(vol => vol.Volba == HlasovaniZastupitele.VolbaHlasovani.Pro) > VolbyZastupitelu.Count / 2;
  }

  public string Text
  {
    get;
    set;
  }

  //[JsonProperty("VolbyZastupitelu", ItemIsReference = true)]
  public ObservableCollection<HlasovaniZastupitele> VolbyZastupitelu
  {
    get;
    set;
  } = new ObservableCollection<HlasovaniZastupitele>();

  public bool ZoBereNaVedomi
  {
    get;
    set;
  }

  #endregion

  public string GenerateTitle(int order, Zasedani zas)
  {
    return $"Č. {order}-{zas.DatumCas.Month}/{zas.DatumCas.Year} - {Text}";
  }
}