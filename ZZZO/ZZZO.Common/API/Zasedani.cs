using System;
using System.Collections.ObjectModel;

namespace ZZZO.Common.API
{
  public class Zasedani
  {
    public DateTime DatumCas { get; set; }

    public int PocetHostu { get; set; }

    public int Poradi { get; set; }

    public string NazevObce { get; set; }

    public Adresa AdresaKonani { get; set; }

    public ObservableCollection<Zastupitel> Zastupitele { get; set; }
  }
}