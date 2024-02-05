using Microsoft.Win32;
using ZZZO.Common;
using ZZZO.Common.API;

namespace ZZZO
{
  public class ZzzoCore : ObservableObject
  {
    #region Proměnné

    private Zasedani _zasedani;

    #endregion

    #region Vlastnosti

    public Zasedani Zasedani
    {
      get => _zasedani;
      set
      {
        if (Equals(value, _zasedani))
        {
          return;
        }

        _zasedani = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(ZasedaniLoaded));
      }
    }

    public bool ZasedaniLoaded
    {
      get => Zasedani != null;
    }

    #endregion

    #region Metody

    public void LoadZasedani()
    {
      OpenFileDialog d = new OpenFileDialog();

      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = "ZZZO soubory (*.zzzo)|*.zzzo";
      d.Title = "Zvolte lokaci pro načtení zasedání ze souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        Zasedani = Zasedani.LoadFromFile(d.FileName);
      }
    }

    public void SaveZasedani()
    {
      if (Zasedani == null)
      {
        return;
      }

      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = "ZZZO soubory (*.zzzo)|*.zzzo";
      d.Title = "Zvolte lokaci pro uložení zasedání do souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        Zasedani.SaveToFile(d.FileName);
      }
    }

    #endregion
  }
}