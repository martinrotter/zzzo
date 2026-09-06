namespace ZZZO.Common.API
{
  public class Adresa : ObservableObject
  {
    private string _cislo = "", _obec = "", _popis = "", _psc = "", _ulice = "";
    public string CisloPopisneOrientacni { get => _cislo; set => SetProperty(ref _cislo, value ?? ""); }
    public string Obec { get => _obec; set => SetProperty(ref _obec, value ?? ""); }
    public string PopisMista { get => _popis; set => SetProperty(ref _popis, value ?? ""); }
    public string Psc { get => _psc; set => SetProperty(ref _psc, value ?? ""); }
    public string Ulice { get => _ulice; set => SetProperty(ref _ulice, value ?? ""); }
    public override string ToString() => $"{Ulice} {CisloPopisneOrientacni}, {Psc} {Obec}";
  }
}
