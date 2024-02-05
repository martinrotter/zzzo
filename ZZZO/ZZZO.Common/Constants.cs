using System.Diagnostics;

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
      public static string AppVersion;

      #endregion

      #region Konstruktory

      static Names()
      {
        FileVersionInfo vi = Utils.GetExecutingAssemblyVersionInfo();

        AppVersion = vi.ProductVersion;
        AppAuthor = vi.CompanyName;
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