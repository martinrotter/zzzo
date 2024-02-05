using System.Globalization;
using System.Threading;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using ZZZO.Common.API;
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

    public Zasedani Zasedani
    {
      get;
      set;
    }

    public bool ZasedaniLoaded
    {
      get => Zasedani != null;
    }

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

    public void LoadZasedani(MainWindow mw)
    {
      OpenFileDialog d = new OpenFileDialog();

      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = "ZZZO soubory (*.zzzo)|*.zzzo";
      d.Title = "Zvolte lokaci pro načtení zasedání ze souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        ResetZasedani(Zasedani.LoadFromFile(d.FileName), mw);
      }
    }

    public void ResetZasedani(Zasedani newZasedani, MainWindow window)
    {
      Zasedani = newZasedani;

      window.DataContext = new MainWindowViewModel(Current.Zasedani, Current);

      window.UcGenerator.DataContext = new GeneratorViewModel(window.UcGenerator, Core);
    }

    public void SaveZasedani()
    {
      if (Zasedani == null)
      {
        return;
      }

      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = "ZZZO soubory (*.zzzo)|*.zzzo";
      d.Title = "Zvolte lokaci pro uložení zasedání do souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        Zasedani.SaveToFile(d.FileName);
      }
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