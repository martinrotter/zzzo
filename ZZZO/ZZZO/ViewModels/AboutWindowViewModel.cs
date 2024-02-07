using System;
using ZZZO.Common;

namespace ZZZO.ViewModels;

public class AboutWindowViewModel : ViewModelBase
{
  #region Vlastnosti

  public string Autor
  {
    get => Constants.Names.AppAuthor;
  }

  public DateTime DatumSestaveni
  {
    get => Constants.Names.AppBuildDate;
  }

  public string Podekovani
  {
    get => $"Některé ikony poskytl {Constants.Names.Freepik}.";
  }

  public string RevizeProgramu
  {
    get => Constants.Names.AppBuildRevision;
  }

  public string VerzeProgramu
  {
    get => Constants.Names.AppVersion;
  }

  #endregion
}