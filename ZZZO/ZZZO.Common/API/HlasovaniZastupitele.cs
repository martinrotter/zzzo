using System.ComponentModel;
using Newtonsoft.Json;
namespace ZZZO.Common.API;

public class HlasovaniZastupitele : ObservableObject
{
  public enum VolbaHlasovani
  {
    [Description("Pro")] Pro,
    [Description("Proti")] Proti,
    [Description("Zdržel se")] ZdrzujeSe
  }
  private VolbaHlasovani _volba = VolbaHlasovani.Pro;
  private Zastupitel _zastupitel;
  public Guid ZastupitelId { get; set; }
  public VolbaHlasovani Volba
  {
    get => _volba;
    set
    {
      if (SetProperty(ref _volba, value))
      {
        OnPropertyChanged(nameof(Pro));
        OnPropertyChanged(nameof(Proti));
        OnPropertyChanged(nameof(ZdrzelSe));
      }
    }
  }
  [JsonIgnore] public bool Pro { get => Volba == VolbaHlasovani.Pro; set { if (value) Volba = VolbaHlasovani.Pro; } }
  [JsonIgnore] public bool Proti { get => Volba == VolbaHlasovani.Proti; set { if (value) Volba = VolbaHlasovani.Proti; } }
  [JsonIgnore] public bool ZdrzelSe { get => Volba == VolbaHlasovani.ZdrzujeSe; set { if (value) Volba = VolbaHlasovani.ZdrzujeSe; } }
  [JsonIgnore] public string JmenoPrijmeniZastupitele => Zastupitel == null ? "Neznámý zastupitel" : $"{Zastupitel.Jmeno} {Zastupitel.Prijmeni}";
  [JsonIgnore] public string Poznamka => Zastupitel?.JePritomen == true ? "" : "nepřítomen";
  [JsonIgnore] public Zastupitel Zastupitel
  {
    get => _zastupitel;
    set
    {
      if (ReferenceEquals(value, _zastupitel)) return;
      if (_zastupitel != null) PropertyChangedEventManager.RemoveHandler(_zastupitel, ZastupitelZmenen, "");
      _zastupitel = value;
      if (value != null)
      {
        ZastupitelId = value.Id;
        PropertyChangedEventManager.AddHandler(value, ZastupitelZmenen, "");
      }
      OnPropertyChanged();
      ZastupitelZmenen(this, null);
    }
  }
  private void ZastupitelZmenen(object sender, PropertyChangedEventArgs e)
  {
    OnPropertyChanged(nameof(JmenoPrijmeniZastupitele));
    OnPropertyChanged(nameof(Poznamka));
  }
}
