using System.Linq;
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

  #endregion

  #region Vlastnosti

  public ICommand AddUsneseniCmd
  {
    get;
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
      OnPropertyChanged();
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