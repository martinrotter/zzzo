using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.API;

namespace ZZZO.ViewModels;

public class ResolutionViewModel : ViewModelBase
{
  #region Proměnné

  private Usneseni _usneseni;
  private ZzzoCore _core;
  private BodProgramu _programEntry;

  #endregion

  #region Vlastnosti

  public ICommand AllAgreeCmd
  {
    get;
  }

  public ICommand AllDisagreeCmd
  {
    get;
  }

  public bool JeEditovatelne
  {
    get => ProgramEntry?.JeEditovatelny == true &&
           (Usneseni == null || !Usneseni.ZoBereNaVedomi);
  }

  public Usneseni Usneseni
  {
    get => _usneseni;
    set
    {
      if (Equals(value, _usneseni))
      {
        return;
      }

      _usneseni = value;

      if (_usneseni != null)
      {
        _usneseni.PropertyChanged += (sender, args) =>
        {
          if (args.PropertyName == nameof(Usneseni.ZoBereNaVedomi))
          {
            OnPropertyChanged(nameof(JeEditovatelne));
          }
        };
      }

      OnPropertyChanged();
      OnPropertyChanged(nameof(JeEditovatelne));
    }
  }

  public ZzzoCore Core
  {
    get => _core;
    set
    {
      if (Equals(value, _core))
      {
        return;
      }

      _core = value;
      OnPropertyChanged();
    }
  }

  public BodProgramu ProgramEntry
  {
    get => _programEntry;
    set
    {
      if (Equals(value, _programEntry))
      {
        return;
      }

      _programEntry = value;

      if (_programEntry != null)
      {
        _programEntry.PropertyChanged += (sender, args) =>
        {
          if (args.PropertyName == nameof(BodProgramu.Typ))
          {
            OnPropertyChanged(nameof(JeEditovatelne));
          }
        };
      }

      OnPropertyChanged();
      OnPropertyChanged(nameof(JeEditovatelne));
    }
  }

  #endregion

  #region Konstruktory

  public ResolutionViewModel(ZzzoCore core)
  {
    Core = core;
    
    AllAgreeCmd = new RelayCommand(obj => MarkAllAgreed(), obj => Usneseni != null);
    AllDisagreeCmd = new RelayCommand(obj => MarkAllDisagreed(), obj => Usneseni != null);
  }

  private void MarkAllDisagreed()
  {
    foreach (HlasovaniZastupitele hlasovaniZastupitele in Usneseni.VolbyZastupitelu)
    {
      hlasovaniZastupitele.Volba = HlasovaniZastupitele.VolbaHlasovani.Proti;
    }
  }

  private void MarkAllAgreed()
  {
    foreach (HlasovaniZastupitele hlasovaniZastupitele in Usneseni.VolbyZastupitelu)
    {
      hlasovaniZastupitele.Volba = HlasovaniZastupitele.VolbaHlasovani.Pro;
    }
  }

  #endregion

}