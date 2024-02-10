using System;
using System.Diagnostics;
using System.IO;
using ZZZO.Common.Properties;

namespace ZZZO.Common
{
  public static class Constants
  {
    #region Vnořené typy

    public static class Names
    {
      #region Statické prvky

      public const string AppLongName = "Zápisník zasedání zastupitelstev obcí";
      public const string AppShortName = "ZZZO";
      public const string Freepik = "www.freepik.com";
      public static string AppAuthor;
      public static DateTime AppBuildDate;
      public static string AppBuildRevision;
      public static string AppVersion;

      #endregion

      #region Konstruktory

      static Names()
      {
        FileVersionInfo vi = Utils.GetExecutingAssemblyVersionInfo();

        AppVersion = vi.ProductVersion;
        AppAuthor = vi.CompanyName;

        AppBuildDate = DateTime.Parse(Resources.build_date);
        AppBuildRevision = Resources.build_commit.Replace("\n", string.Empty);
      }

      #endregion
    }

    public static class PathsAndFiles
    {
      #region Statické prvky

      public const string ZzzoFileSuffix = "zzzo";
      public static string AppBaseFolder;
      public static string AppDataFolder;
      public static string AppStylesFolder;
      public static string AppTinyMceFolder;

      #endregion

      #region Konstruktory

      static PathsAndFiles()
      {
        AppBaseFolder = Utils.GetExecutingAssemblyFolder();
        AppDataFolder = Path.Combine(AppBaseFolder, "Data");
        AppStylesFolder = Path.Combine(AppDataFolder, "Styles");
        AppTinyMceFolder = Path.Combine(AppDataFolder, "TinyMceEditor");
      }

      #endregion
    }

    public static class Uris
    {
      #region Statické prvky

      public const string Document = "https://document.zzzo";

      #endregion
    }

    #endregion
  }
}