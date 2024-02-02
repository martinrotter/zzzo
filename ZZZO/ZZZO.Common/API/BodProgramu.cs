using System.Collections.ObjectModel;

namespace ZZZO.Common.API;

public class BodProgramu : ObservableObject
{
  #region Proměnné

  private bool _jeDoplneny;
  private bool _jePodbod;
  private string _nadpis;
  private string _nadpisPoradi;
  private bool _schvalovaniProgramu;
  private string _text;
  private ObservableCollection<Usneseni> _usneseni = new ObservableCollection<Usneseni>();

  #endregion

  #region Vlastnosti

  public bool JeDoplneny
  {
    get => _jeDoplneny;
    set
    {
      if (value == _jeDoplneny)
      {
        return;
      }

      _jeDoplneny = value;
      OnPropertyChanged();
    }
  }

  public bool JePodbod
  {
    get => _jePodbod;
    set
    {
      if (value == _jePodbod)
      {
        return;
      }

      _jePodbod = value;
      OnPropertyChanged();
    }
  }

  public string Nadpis
  {
    get => _nadpis;
    set
    {
      if (value == _nadpis)
      {
        return;
      }

      _nadpis = value;
      OnPropertyChanged();
    }
  }

  public string NadpisPoradi
  {
    get => _nadpisPoradi;
    set
    {
      if (value == _nadpisPoradi)
      {
        return;
      }

      _nadpisPoradi = value;
      OnPropertyChanged();
    }
  }

  public bool SchvalovaniProgramu
  {
    get => _schvalovaniProgramu;
    set
    {
      if (value == _schvalovaniProgramu)
      {
        return;
      }

      _schvalovaniProgramu = value;
      OnPropertyChanged();
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

  public ObservableCollection<Usneseni> Usneseni
  {
    get => _usneseni;
    set
    {
      if (Equals(value, _usneseni))
      {
        return;
      }

      _usneseni = value;
      OnPropertyChanged();
    }
  }

  #endregion
}