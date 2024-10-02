using System.Collections.ObjectModel;

namespace ZZZO.Common.API
{
  public class Program : ObservableObject
  {
    #region Proměnné

    private ObservableCollection<BodProgramu> _bodyProgramu = new ObservableCollection<BodProgramu>();

    #endregion

    #region Vlastnosti

    public ObservableCollection<BodProgramu> BodyProgramu
    {
      get => _bodyProgramu;
      set
      {
        if (Equals(value, _bodyProgramu))
        {
          return;
        }

        _bodyProgramu = value;
        OnPropertyChanged();
      }
    }

    #endregion

    #region Metody

    public BodProgramu VygenerovatBodProgramu(Zasedani zas, BodProgramu.TypBoduProgramu typProgramu, bool bezUsneseni = false)
    {
      BodProgramu bod = new BodProgramu();

      bod.Typ = typProgramu;

      switch (typProgramu)
      {
        case BodProgramu.TypBoduProgramu.SchvaleniZapisOver:
          bod.Nadpis = "Schválení zapisovatele a ověřovatelů zápisu";

          zas.AddUsneseni(bod, new Usneseni
          {
            Text = bod.Nadpis
          });

          break;

        case BodProgramu.TypBoduProgramu.SchvaleniProgramu:
          bod.Nadpis = "Schválení programu";

          zas.AddUsneseni(bod, new Usneseni
          {
            Text = bod.Nadpis
          });

          break;

        case BodProgramu.TypBoduProgramu.KontrolaMinulehoZapisu:
          bod.Nadpis = "Kontrola zápisu a plnění usnesení z minulého zasedání ZO";
          bod.Text = "Starosta obce zhodnotil program z minulého jednání ZO a " +
                     "informoval přítomné zastupitele i veřejnost o " +
                     "projednaných bodech a splněných úkolech.";
          break;

        case BodProgramu.TypBoduProgramu.BodZasedani:
        case BodProgramu.TypBoduProgramu.DoplnenyBodZasedani:
          bod.Nadpis = "Bod zasedání";

          zas.AddUsneseni(bod, new Usneseni
          {
            Text = "Usnesení z tohoto bodu zasedání"
          });

          break;
      }

      if (bezUsneseni)
      {
        bod.Usneseni.Clear();
      }

      return bod;
    }

    #endregion
  }
}