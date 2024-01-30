namespace ZZZO.Common.API
{
  public class Adresa
  {
    #region Vlastnosti

    public override string ToString()
    {
      return $"{Ulice} {CisloPopisneOrientacni}, {Psc} {Obec}";
    }

    public string CisloPopisneOrientacni
    {
      get;
      set;
    }

    public string Obec
    {
      get;
      set;
    }

    public string PopisMista
    {
      get;
      set;
    }

    public string Psc
    {
      get;
      set;
    }

    public string Ulice
    {
      get;
      set;
    }

    #endregion
  }
}