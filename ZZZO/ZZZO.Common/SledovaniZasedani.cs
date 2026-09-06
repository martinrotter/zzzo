using System.Collections.Specialized;
using System.ComponentModel;
using ZZZO.Common.API;

namespace ZZZO.Common;

public sealed class SledovaniZasedani : IDisposable
{
  private readonly Zasedani _zasedani;
  private readonly Action _zmena;
  private readonly HashSet<INotifyPropertyChanged> _objekty = new();
  private readonly HashSet<INotifyCollectionChanged> _kolekce = new();
  public SledovaniZasedani(Zasedani zasedani, Action zmena) { _zasedani = zasedani; _zmena = zmena; Pripojit(); }

  private void Pripojit()
  {
    Dispose();
    void Objekt(INotifyPropertyChanged o) { if (o != null && _objekty.Add(o)) o.PropertyChanged += ZmenenaVlastnost; }
    void Kolekce(INotifyCollectionChanged k) { if (k != null && _kolekce.Add(k)) k.CollectionChanged += ZmenenaKolekce; }
    Objekt(_zasedani); Objekt(_zasedani.AdresaKonani); Objekt(_zasedani.Program);
    Kolekce(_zasedani.Zastupitele); Kolekce(_zasedani.Program.BodyProgramu);
    foreach (var z in _zasedani.Zastupitele) Objekt(z);
    foreach (var b in _zasedani.Program.VsechnyBody())
    {
      Objekt(b); Kolekce(b.Usneseni); Kolekce(b.Podbody);
      foreach (var u in b.Usneseni)
      {
        Objekt(u); Kolekce(u.VolbyZastupitelu);
        foreach (var h in u.VolbyZastupitelu) Objekt(h);
      }
    }
  }
  private void ZmenenaVlastnost(object sender, PropertyChangedEventArgs e)
  {
    if (sender is HlasovaniZastupitele && e.PropertyName != nameof(HlasovaniZastupitele.Volba)) return;
    if (sender is Usneseni && e.PropertyName == nameof(Usneseni.Vytah)) return;
    if (sender is Zasedani && e.PropertyName == nameof(Zasedani.VystupniSoubor)) return;
    if (sender is Zasedani && e.PropertyName is nameof(Zasedani.Program) or nameof(Zasedani.AdresaKonani) or nameof(Zasedani.Zastupitele)) Pripojit();
    _zmena();
  }
  private void ZmenenaKolekce(object sender, NotifyCollectionChangedEventArgs e) { Pripojit(); _zmena(); }
  public void Dispose()
  {
    foreach (var o in _objekty) o.PropertyChanged -= ZmenenaVlastnost;
    foreach (var k in _kolekce) k.CollectionChanged -= ZmenenaKolekce;
    _objekty.Clear(); _kolekce.Clear();
  }
}
