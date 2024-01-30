using System.Collections.ObjectModel;

namespace ZZZO.Common.API
{
  public class Program
  {
    #region Vlastnosti

    public ObservableCollection<BodProgramu> BodyProgramu
    {
      get;
      set;
    } = new ObservableCollection<BodProgramu>();

    #endregion
  }
}