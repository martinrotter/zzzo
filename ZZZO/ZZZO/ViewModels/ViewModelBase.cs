using System.Windows;
using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common;

namespace ZZZO.ViewModels
{
  public class ViewModelBase : ObservableObject
  {
    #region Vlastnosti

    public ICommand CloseWindowCommand
    {
      get;
    }

    #endregion

    #region Konstruktory

    public ViewModelBase()
    {
      CloseWindowCommand = new RelayCommand(o => (o as Window).Close(), o => true);
    }

    #endregion
  }
}