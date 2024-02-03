using System.Windows.Media;

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
      public static string AppVersion;
      public static string AppAuthor;

      #endregion

      #region Konstruktory

      static Names()
      {
        var vi =  Utils.GetExecutingAssemblyVersionInfo();

        AppVersion = vi.ProductVersion;
        AppAuthor = vi.CompanyName;
      }

      #endregion
    }

    #endregion
  }
}