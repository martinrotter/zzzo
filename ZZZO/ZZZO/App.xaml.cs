using System.Globalization;
using System.Threading;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using ZZZO.Controls;
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

      var ci = new CultureInfo("cs-CZ");

      CultureInfo.CurrentCulture = ci;
      CultureInfo.CurrentUICulture = ci;
      CultureInfo.DefaultThreadCurrentCulture = ci;
      Thread.CurrentThread.CurrentCulture = ci;
      Thread.CurrentThread.CurrentUICulture = ci;
    }

    #endregion

    #region Metody

    public void SetDataContexts(MainWindow window)
    {
      window.DataContext = new MainWindowViewModel(window, Current, Core);
      window.UcBasicInfo.DataContext = new BasicInfoViewModel(Core);
      window.UcProgram.DataContext = new ProgramViewModel(Core);
      window.UcGenerator.DataContext = new GeneratorViewModel(Core);
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