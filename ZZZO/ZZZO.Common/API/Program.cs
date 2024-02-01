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
  }
}