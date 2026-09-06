using System.ComponentModel;
using ZZZO.Common.API;

namespace ZZZO.Common.Generators
{
  public abstract class Generator : ObservableObject
  {
    #region Enumy

    public enum TypDokumentu
    {
      [Description("Zápis")]
      Zapis = 1,

      [Description("Pozvánka")]
      Pozvanka = 2
    }

    #endregion

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

    public abstract string Title
    {
      get;
    }

    #endregion

    #region Metody

    public async Task<byte[]> Generate(Zasedani zasedani, IProgress<int> progress, object param = null)
    {
      if (IsGenerating)
      {
        throw new InvalidOperationException("Generování již probíhá.");
      }
      else
      {
        IsGenerating = true;
      }

      try { return await Task.Run(() => GenerateDoWork(zasedani, progress, param)); }
      finally { IsGenerating = false; }
    }

    protected abstract byte[] GenerateDoWork(Zasedani zas, IProgress<int> progress, object param);

    #endregion
  }
}
