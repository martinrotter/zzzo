using System;
using System.Threading.Tasks;
using ZZZO.Common.API;

namespace ZZZO.Common.Generators
{
  public abstract class Generator : ObservableObject
  {
    #region Proměnné

    private bool _isGenerating;

    #endregion

    #region Vlastnosti

    public abstract string FileSuffix
    {
      get;
    }

    public bool IsGenerating
    {
      get => _isGenerating;
      set
      {
        if (value == _isGenerating)
        {
          return;
        }

        _isGenerating = value;
        OnPropertyChanged();
      }
    }

    #endregion

    #region Metody

    public Task<byte[]> Generate(Zasedani zasedani, IProgress<int> progress)
    {
      if (IsGenerating)
      {
        return null;
      }
      else
      {
        IsGenerating = true;
      }

      Task<byte[]> tsk = Task.Run(() => GenerateDoWork(zasedani, progress));

      tsk.ContinueWith(task => IsGenerating = false);

      return tsk;
    }

    protected abstract byte[] GenerateDoWork(Zasedani zas, IProgress<int> progress);

    #endregion
  }
}