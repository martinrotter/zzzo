using Newtonsoft.Json;

namespace ZZZO.Common.API;

public class Zastupitel : ObservableObject
{
  #region Proměnné

  private bool _jeOverovatel;
  private bool _jePritomen;
  private bool _jeRidici;
  private bool _jeStarosta;
  private bool _jeZapisovatel;
  private string _jmeno;
  private string _prijmeni;

  #endregion

  #region Vlastnosti

  public bool JeOverovatel
  {
    get => _jeOverovatel;
    set
    {
      if (value == _jeOverovatel)
      {
        return;
      }

      _jeOverovatel = value;
      OnPropertyChanged();
    }
  }

  public bool JePritomen
  {
    get => _jePritomen;
    set
    {
      if (value == _jePritomen)
      {
        return;
      }

      _jePritomen = value;
      OnPropertyChanged();
    }
  }

  public bool JeRidici
  {
    get => _jeRidici;
    set
    {
      if (value != _jeRidici)
      {
        _jeRidici = value;

        /*
        if (value)
        {
          foreach (Zastupitel zastupitel in Zasedani.Zastupitele)
          {
            if (!ReferenceEquals(zastupitel, this))
            {
              zastupitel._jeRidici = false;
              zastupitel.OnPropertyChanged();
            }
          }
        }
        */

        OnPropertyChanged();
      }
    }
  }

  public bool JeStarosta
  {
    get => _jeStarosta;
    set
    {
      if (value != _jeStarosta)
      {
        _jeStarosta = value;

        /*
        if (value)
        {
          foreach (Zastupitel zastupitel in Zasedani.Zastupitele)
          {
            if (!ReferenceEquals(zastupitel, this))
            {
              zastupitel._jeStarosta = false;
              zastupitel.OnPropertyChanged();
            }
          }
        }
        */

        OnPropertyChanged();
      }
    }
  }

  public bool JeZapisovatel
  {
    get => _jeZapisovatel;
    set
    {
      if (value != _jeZapisovatel)
      {
        _jeZapisovatel = value;

        /*
        if (value)
        {
          foreach (Zastupitel zastupitel in Zasedani.Zastupitele)
          {
            if (!ReferenceEquals(zastupitel, this))
            {
              zastupitel._jeZapisovatel = false;
              zastupitel.OnPropertyChanged();
            }
          }
        }
        */

        OnPropertyChanged();
      }
    }
  }

  public string Jmeno
  {
    get => _jmeno;
    set
    {
      if (value == _jmeno)
      {
        return;
      }

      _jmeno = value;
      OnPropertyChanged();
    }
  }

  public string Prijmeni
  {
    get => _prijmeni;
    set
    {
      if (value == _prijmeni)
      {
        return;
      }

      _prijmeni = value;
      OnPropertyChanged();
    }
  }

  #endregion
}