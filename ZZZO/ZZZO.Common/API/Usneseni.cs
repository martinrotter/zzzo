using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
      IEnumerable<HlasovaniZastupitele> pritomni = VolbyZastupitelu.Where(zas => zas.Zastupitel.JePritomen);

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
      OnPropertyChanged(nameof(ZoNebereNaVedomi));
    }
  }

  public bool ZoNebereNaVedomi
  {
    get => !ZoBereNaVedomi;
  }

  #endregion

  #region Konstruktory

  public Usneseni()
  {
    _volbyZastupitelu.CollectionChanged += (sender, args) =>
    {
      if (args.Action != NotifyCollectionChangedAction.Add || args.NewItems == null)
      {
        return;
      }

      foreach (HlasovaniZastupitele hlas in args.NewItems)
      {
        hlas.PropertyChanged += (o, eventArgs) => { OnPropertyChanged(nameof(JeSchvaleno)); };
      }
    };
  }

  #endregion

  #region Metody

  public string GenerateTitle(int order, Zasedani zas)
  {
    return $"Č. {order}-{zas.DatumCasKonani.Month}/{zas.DatumCasKonani.Year} - {Text}";
  }

  #endregion
}