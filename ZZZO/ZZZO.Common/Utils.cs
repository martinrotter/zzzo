using System.Diagnostics;
using System.Reflection;

namespace ZZZO.Common
{
  public static class Utils
  {
    #region Metody

    public static string GetExecutingAssemblyVersion()
    {
      return FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion;
    }

    #endregion
  }
}