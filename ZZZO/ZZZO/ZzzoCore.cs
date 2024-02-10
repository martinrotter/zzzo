using System.IO;
using System.Linq;
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

        ZasedaniOriginalData = _zasedani.ToJson();

        OnPropertyChanged();
        OnPropertyChanged(nameof(ZasedaniLoaded));
      }
    }

    public bool ZasedaniIsDirty
    {
      get => ZasedaniLoaded && ZasedaniOriginalData != null && !Zasedani.ToJson().SequenceEqual(ZasedaniOriginalData);
    }

    public bool ZasedaniLoaded
    {
      get => Zasedani != null;
    }

    /// <summary>
    /// Is used to determine if there is any change or not.
    /// </summary>
    private byte[] ZasedaniOriginalData
    {
      get;
      set;
    }

    #endregion

    #region Metody

    public static string ChooseLoadFile(string fileSuffix, string title = null)
    {
      OpenFileDialog d = new OpenFileDialog();

      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{fileSuffix.ToUpper()} soubory (*.{fileSuffix})|*.{fileSuffix}";
      d.Title = title ?? $"Zvolte lokaci pro načtení zápisu zasedání ze {fileSuffix.ToUpper()} souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        return d.FileName;
      }
      else
      {
        return null;
      }
    }

    public static string ChooseSaveFile(Zasedani zasedani, string fileSuffix, string title = null)
    {
      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{fileSuffix.ToUpper()} soubory (*.{fileSuffix})|*.{fileSuffix}";
      d.Title = title ?? $"Zvolte lokaci pro uložení zápisu zasedání do {fileSuffix.ToUpper()} souboru";

      if (zasedani != null && !string.IsNullOrWhiteSpace(zasedani.VystupniSoubor))
      {
        d.FileName = zasedani.VystupniSoubor + $".{fileSuffix}";
      }

      if (d.ShowDialog().GetValueOrDefault())
      {
        if (zasedani != null)
        {
          string chosenDir = Path.GetDirectoryName(d.FileName);
          string chosenFile = Path.GetFileNameWithoutExtension(d.FileName);

          zasedani.VystupniSoubor = Path.Combine(chosenDir, chosenFile);
        }

        return d.FileName;
      }
      else
      {
        return null;
      }
    }

    public bool LoadZasedani()
    {
      string filePath = ChooseLoadFile(Constants.PathsAndFiles.ZzzoFileSuffix);

      if (File.Exists(filePath))
      {
        Zasedani = Zasedani.LoadFromFile(filePath);
        return true;
      }
      else
      {
        return false;
      }
    }

    public void NewZaseDani(Zasedani zas)
    {
      Zasedani = zas;
    }

    public bool SaveZasedani()
    {
      if (Zasedani == null)
      {
        return false;
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
        ZasedaniOriginalData = Zasedani.ToJson();
        return true;
      }
      else
      {
        return false;
      }
    }

    #endregion
  }
}