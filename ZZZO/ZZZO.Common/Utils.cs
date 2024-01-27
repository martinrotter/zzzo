using System.Diagnostics;
using System.Reflection;

namespace ZZZO.Common
{
  public static class Utils
  {
    public static string GetExecutingAssemblyVersion()
    {
      return FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion;
    }
  }
}