using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.API;

namespace ZZZO.ViewModels;

public class ProgramEntryViewModel : ViewModelBase
{
  #region Proměnné

  private Usneseni _chosenUsneseni;
  private ZzzoCore _core;
  private BodProgramu _programEntry;
  private ResolutionViewModel _resolutionViewModel;

  #endregion

  #region Vlastnosti

  public ICommand AddUsneseniCmd
  {
    get;
  }

  public ResolutionViewModel ResolutionViewModel
  {
    get => _resolutionViewModel;
    set
    {
      if (Equals(value, _resolutionViewModel))
      {
        return;
      }

      _resolutionViewModel = value;
      OnPropertyChanged();
    }
  }

  public IEnumerable<BodProgramu.TypBoduProgramu> TypyBoduProgramu
  {
    get => Enum.GetValues<BodProgramu.TypBoduProgramu>();
  }

  public bool LzeEditovatUsneseni
  {
    get => _programEntry != null && _programEntry.JeEditovatelny && _programEntry.Usneseni.Count > 0;
  }

  public Usneseni ChosenUsneseni
  {
    get => _chosenUsneseni;
    set
    {
      if (Equals(value, _chosenUsneseni))
      {
        return;
      }

      _chosenUsneseni = value;

      ResolutionViewModel.ProgramEntry = _programEntry;
      ResolutionViewModel.Usneseni = _chosenUsneseni;
      
      OnPropertyChanged();
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
          if (args.PropertyName == nameof(BodProgramu.JeEditovatelny))
          {
            OnPropertyChanged(nameof(LzeEditovatUsneseni));
          }
        };

        if (_programEntry.Usneseni != null)
        {
          _programEntry.Usneseni.CollectionChanged += (sender, args) => { OnPropertyChanged(nameof(LzeEditovatUsneseni)); };
        }
      }
      
      OnPropertyChanged();
      OnPropertyChanged(nameof(LzeEditovatUsneseni));
    }
  }

  public ICommand RemoveUsneseniCmd
  {
    get;
  }

  #endregion

  #region Konstruktory

  public ProgramEntryViewModel(ZzzoCore core)
  {
    Core = core;
    ResolutionViewModel = new ResolutionViewModel(core);

    AddUsneseniCmd = new RelayCommand(obj => AddUsneseni(), obj => true);
    RemoveUsneseniCmd = new RelayCommand(obj => RemoveUsneseni(), obj => ChosenUsneseni != null);
  }

  #endregion

  #region Metody

  private void AddUsneseni()
  {
    Core.Zasedani.AddUsneseni(ProgramEntry, new Usneseni
    {
      Text = "Text usnesení"
    });

    if (ProgramEntry.Usneseni.Count == 1)
    {
      ChosenUsneseni = ProgramEntry.Usneseni[0];
    }
  }

  private void RemoveUsneseni()
  {
    if (ChosenUsneseni != null)
    {
      ProgramEntry.Usneseni.Remove(ChosenUsneseni);
    }
  }

  #endregion
}