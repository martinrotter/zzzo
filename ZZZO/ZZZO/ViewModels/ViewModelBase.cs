using System.Windows;
using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common;

namespace ZZZO.ViewModels
{
  public class ViewModelBase : ObservableObject
  {
    #region Vlastnosti

    public ICommand CloseWindowCmd
    {
      get;
    }

    #endregion

    #region Konstruktory

    public ViewModelBase()
    {
      CloseWindowCmd = new RelayCommand(o => {
        (o as Window)?.Close();
      }, o => true);
    }

    #endregion
  }
}