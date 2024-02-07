using System;
using System.Windows.Input;

namespace ZZZO.Commands
{
  public class RelayCommand<T> : ICommand
  {
    #region Proměnné

    private readonly Func<T, bool> canExecute;
    private readonly Action<T> execute;

    #endregion

    #region Konstruktory

    public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
    {
      this.execute = execute;
      this.canExecute = canExecute;
    }

    #endregion

    #region Implementace rozhraní

    public bool CanExecute(object parameter)
    {
      return canExecute == null || canExecute((T)parameter);
    }

    public event EventHandler CanExecuteChanged
    {
      add => CommandManager.RequerySuggested += value;
      remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object parameter)
    {
      execute((T)parameter);
    }

    #endregion
  }

  public class RelayCommandEmpty : ICommand
  {
    #region Proměnné

    private readonly Func<bool> canExecute;
    private readonly Action execute;

    #endregion

    #region Konstruktory

    public RelayCommandEmpty(Action execute, Func<bool> canExecute = null)
    {
      this.execute = execute;
      this.canExecute = canExecute;
    }

    #endregion

    #region Implementace rozhraní

    public bool CanExecute(object parameter)
    {
      return canExecute == null || canExecute();
    }

    public event EventHandler CanExecuteChanged
    {
      add => CommandManager.RequerySuggested += value;
      remove => CommandManager.RequerySuggested -= value;
    }

    public void Execute(object parameter)
    {
      execute();
    }

    #endregion
  }

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