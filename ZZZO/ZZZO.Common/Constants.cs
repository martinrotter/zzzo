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

      #endregion

      #region Konstruktory

      static Names()
      {
        AppVersion = Utils.GetExecutingAssemblyVersion();
      }

      #endregion
    }

    #endregion
  }
}