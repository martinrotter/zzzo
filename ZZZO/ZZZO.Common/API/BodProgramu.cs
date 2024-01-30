using System.Collections.ObjectModel;

namespace ZZZO.Common.API;

public class BodProgramu
{
  #region Vlastnosti

  public bool JeDoplneny
  {
    get;
    set;
  }

  public bool JePodbod
  {
    get;
    set;
  }

  public bool SchvalovaniProgramu
  {
    get;
    set;
  }

  public string Nadpis
  {
    get;
    set;
  }

  public string Text
  {
    get;
    set;
  }

  public ObservableCollection<Usneseni> Usneseni
  {
    get;
    set;
  } = new ObservableCollection<Usneseni>();

  #endregion
}