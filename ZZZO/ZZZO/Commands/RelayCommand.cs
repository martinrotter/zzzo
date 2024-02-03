using System;
using System.Windows.Input;

namespace ZZZO.Commands
{
  public class RelayCommand : ICommand
  {
    #region Proměnné

    private readonly Func<object, bool> canExecute;
    private readonly Action<object> execute;

    #endregion

    #region Konstruktory

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
      this.execute = execute;
      this.canExecute = canExecute;
    }

    #endregion

    #region Implementace rozhraní

    public bool CanExecute(object parameter)
    {
      return canExecute == null || canExecute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
      add => CommandManager.RequerySuggested += value;
      remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object parameter)
    {
      execute(parameter);
    }

    #endregion
  }
}