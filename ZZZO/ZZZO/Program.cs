using CefSharp.BrowserSubprocess;

namespace ZZZO
{
  internal static class Program
  {
    [STAThread]
    public static int Main(string[] args)
    {
      if (args.Length == 4 && args[0] == Common.Generators.PrevodnikWord.Argument)
        return Common.Generators.PrevodnikWord.Spustit(args[1], args[2], args[3]);

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
