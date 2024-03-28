using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ZZZO.Common.API;

public class BodProgramu : ObservableObject
{
  #region Enumy

  public enum TypBoduProgramu
  {
    [Description("Schvalování zapisovatele/ověřovatelů")]
    SchvaleniZapisOver = 0,

    [Description("Schvalování programu zasedání")]
    SchvaleniProgramu = 1,

    [Description("Bod zasedání")]
    BodZasedani = 2,

    [Description("Doplněný bod zasedání")]
    DoplnenyBodZasedani = 3
  }

  #endregion

  public bool JeEditovatelny
  {
    get => Typ == TypBoduProgramu.DoplnenyBodZasedani ||
           Typ == TypBoduProgramu.BodZasedani;
  }

  #region Proměnné

  private bool _jePodbod;
  private string _nadpis;
  private string _nadpisPoradi;
  private string _text;
  private TypBoduProgramu _typ;
  private ObservableCollection<Usneseni> _usneseni = new ObservableCollection<Usneseni>();

  #endregion

  #region Vlastnosti

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

  public TypBoduProgramu Typ
  {
    get => _typ;
    set
    {
      if (value == _typ)
      {
        return;
      }

      _typ = value;

      OnPropertyChanged();
      OnPropertyChanged(nameof(JeEditovatelny));
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

  #region Konstruktory

  public BodProgramu()
  {
    Typ = TypBoduProgramu.BodZasedani;
  }

  #endregion
}