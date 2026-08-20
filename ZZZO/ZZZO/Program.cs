using CefSharp.BrowserSubprocess;

namespace ZZZO
{
  internal static class Program
  {
    [STAThread]
    public static int Main(string[] args)
    {
      int exitCode = SelfHost.Main(args);

      if (exitCode >= 0)
      {
        return exitCode;
      }

      App app = new App();
      app.InitializeComponent();
      return app.Run();
    }
  }
}
