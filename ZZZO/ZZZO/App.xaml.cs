using System.Globalization;
using System.Threading;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using ZZZO.ViewModels;
using ZZZO.Windows;

namespace ZZZO
{
  public partial class App : Application
  {
    #region Vlastnosti

    public new static App Current
    {
      get;
      private set;
    }

    public ZzzoCore Core
    {
      get;
    } = new ZzzoCore();

    #endregion

    #region Konstruktory

    public App()
    {
      Current = this;

      CultureInfo.CurrentCulture = new CultureInfo("cs-CZ");
      CultureInfo.CurrentUICulture = new CultureInfo("cs-CZ");
      CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("cs-CZ");
      Thread.CurrentThread.CurrentCulture = new CultureInfo("cs-CZ");
      Thread.CurrentThread.CurrentUICulture = new CultureInfo("cs-CZ");
    }

    #endregion

    #region Metody

    public void SetDataContexts(MainWindow window)
    {
      window.DataContext = new MainWindowViewModel(Current, Core);
      window.UcBasicInfo.DataContext = new BasicInfoViewModel(Core);
      window.UcProgram.DataContext = new ProgramViewModel(Core);
      window.UcGenerator.DataContext = new GeneratorViewModel(window.UcGenerator, Core);
    }

    protected override void OnExit(ExitEventArgs e)
    {
      base.OnExit(e);
      Cef.Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      Cef.Initialize(new CefSettings());
    }

    #endregion
  }
}