using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ZZZO.Common
{
  public static class Utils
  {
    #region Metody

    public static FileVersionInfo GetExecutingAssemblyVersionInfo()
    {
      return FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
    }

    #endregion
  }
}