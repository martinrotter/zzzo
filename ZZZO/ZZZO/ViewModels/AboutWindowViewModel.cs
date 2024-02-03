using ZZZO.Common;

namespace ZZZO.ViewModels;

public class AboutWindowViewModel : ViewModelBase
{
  #region Vlastnosti

  public string Autor
  {
    get => Constants.Names.AppAuthor;
  }

  public string Podekovani
  {
    get => $"Některé ikony poskytl {Constants.Names.Freepik}.";
  }

  public string VerzeProgramu
  {
    get => Constants.Names.AppVersion;
  }

  #endregion

  #region Konstruktory

  public AboutWindowViewModel()
  {
  }

  #endregion
}