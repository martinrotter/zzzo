using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.API;

namespace ZZZO.ViewModels;

public class ProgramViewModel : ViewModelBase
{
  #region Proměnné

  private BodProgramu _chosenProgramEntry;

  private ZzzoCore _core;
  private ProgramEntryViewModel _entryViewModel;

  #endregion

  #region Vlastnosti

  public ICommand AddProgramEntryCmd
  {
    get;
  }

  public bool ChosenFirstProgramEntry
  {
    get => ChosenProgramEntry != null && Core?.Zasedani?.Program?.BodyProgramu?.IndexOf(ChosenProgramEntry) == 0;
  }

  public bool ChosenLastProgramEntry
  {
    get => ChosenProgramEntry != null &&
           Core?.Zasedani?.Program?.BodyProgramu?.IndexOf(ChosenProgramEntry) == Core.Zasedani.Program.BodyProgramu.Count - 1;
  }

  public BodProgramu ChosenProgramEntry

  {
    get => _chosenProgramEntry;
    set
    {
      if (Equals(value, _chosenProgramEntry))
      {
        return;
      }

      _chosenProgramEntry = value;

      EntryViewModel.ProgramEntry = _chosenProgramEntry;

      OnPropertyChanged();
      OnPropertyChanged(nameof(ChosenFirstProgramEntry));
      OnPropertyChanged(nameof(ChosenLastProgramEntry));
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

  public ProgramEntryViewModel EntryViewModel
  {
    get => _entryViewModel;
    set
    {
      if (Equals(value, _entryViewModel))
      {
        return;
      }

      _entryViewModel = value;
      OnPropertyChanged();
    }
  }

  public ICommand MoveProgramEntryDownCmd
  {
    get;
  }

  public ICommand MoveProgramEntryUpCmd
  {
    get;
  }

  public ICommand RemoveProgramEntryCmd
  {
    get;
  }

  #endregion

  #region Konstruktory

  public ProgramViewModel(ZzzoCore core)
  {
    Core = core;
    EntryViewModel = new ProgramEntryViewModel(core);

    AddProgramEntryCmd = new RelayCommand(obj => AddProgramEntry(), obj => true);
    RemoveProgramEntryCmd = new RelayCommand(obj => RemoveProgramEntry(), obj => ChosenProgramEntry != null);
    MoveProgramEntryUpCmd = new RelayCommand(obj => MoveProgramEntryUp(), obj => ChosenProgramEntry != null &&
                                                                                 !ChosenFirstProgramEntry);
    MoveProgramEntryDownCmd = new RelayCommand(obj => MoveProgramEntryDown(), obj => ChosenProgramEntry != null &&
                                                                                     !ChosenLastProgramEntry);
  }

  #endregion

  #region Metody

  private void AddProgramEntry()
  {
    Core.Zasedani.Program.BodyProgramu.Add(new BodProgramu
    {
      Nadpis = "Nový bod programu",
      Text = "Text bodu programu"
    });

    if (Core.Zasedani.Program.BodyProgramu.Count == 1)
    {
      ChosenProgramEntry = Core.Zasedani.Program.BodyProgramu[0];
    }
  }

  private void MoveProgramEntryDown()
  {
    if (ChosenProgramEntry != null)
    {
      int idx = Core.Zasedani.Program.BodyProgramu.IndexOf(ChosenProgramEntry);

      if (idx < Core.Zasedani.Program.BodyProgramu.Count - 1)
      {
        Core.Zasedani.Program.BodyProgramu.Move(idx, idx + 1);
      }
    }
  }

  private void MoveProgramEntryUp()
  {
    if (ChosenProgramEntry != null)
    {
      int idx = Core.Zasedani.Program.BodyProgramu.IndexOf(ChosenProgramEntry);

      if (idx > 0)
      {
        Core.Zasedani.Program.BodyProgramu.Move(idx, idx - 1);
      }
    }
  }

  private void RemoveProgramEntry()
  {
    if (ChosenProgramEntry != null)
    {
      Core.Zasedani.Program.BodyProgramu.Remove(ChosenProgramEntry);
    }
  }

  #endregion
}