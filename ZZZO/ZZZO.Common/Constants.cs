namespace ZZZO.Common
{
  public static class Constants
  {
    public static class Names
    {
      public const string AppShortName = "ZZZO";
      public const string AppLongName = "Zápisník zasedání zastupitelstev obcí";
      public const string Freepik = "www.freepik.com";
      public static string AppVersion;

      static Names()
      {
        AppVersion = Utils.GetExecutingAssemblyVersion();
      }
    }
  }
}