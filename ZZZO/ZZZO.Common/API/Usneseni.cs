using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class Usneseni : ObservableObject
{
  #region Proměnné

  private string _text;
  private ObservableCollection<HlasovaniZastupitele> _volbyZastupitelu = new ObservableCollection<HlasovaniZastupitele>();
  private bool _zoBereNaVedomi;

  #endregion

  #region Vlastnosti

  [JsonIgnore]
  public bool JeSchvaleno
  {
    get
    {
      var pritomni = VolbyZastupitelu.Where(zas => zas.Zastupitel.JePritomen);

      return pritomni.Count(zas => zas.Volba == HlasovaniZastupitele.VolbaHlasovani.Pro) >
             pritomni.Count() / 2;
    }
  }

  public string Text
  {
    get => _text;
    set
    {
      if (value == _text)
      {
        return;
      }

      _text = value;
      OnPropertyChanged();
    }
  }

  //[JsonProperty("VolbyZastupitelu", ItemIsReference = true)]
  public ObservableCollection<HlasovaniZastupitele> VolbyZastupitelu
  {
    get => _volbyZastupitelu;
    set
    {
      if (Equals(value, _volbyZastupitelu))
      {
        return;
      }

      _volbyZastupitelu = value;
      OnPropertyChanged();
    }
  }

  public bool ZoBereNaVedomi
  {
    get => _zoBereNaVedomi;
    set
    {
      if (value == _zoBereNaVedomi)
      {
        return;
      }

      _zoBereNaVedomi = value;
      OnPropertyChanged();
    }
  }

  #endregion

  #region Metody

  public string GenerateTitle(int order, Zasedani zas)
  {
    return $"Č. {order}-{zas.DatumCasKonani.Month}/{zas.DatumCasKonani.Year} - {Text}";
  }

  #endregion
}