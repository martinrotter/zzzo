using System.Globalization;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using ZZZO.Common.API;

namespace ZZZO
{
  public partial class App : Application
  {
    public App()
    {
      Current = this;

      CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("cs-CZ");
      Thread.CurrentThread.CurrentCulture = new CultureInfo("cs-CZ");
      Thread.CurrentThread.CurrentUICulture = new CultureInfo("cs-CZ");
    }

    public Zasedani Zasedani { get; set; }

    public bool ZasedaniLoaded => Zasedani != null;

    public new static App Current { get; private set; }

    public void ResetZasedani(Zasedani newZasedani, MainWindow window)
    {
      Zasedani = newZasedani;

      window.DataContext = new
      {
        Zasedani = Current.Zasedani,
        App = App.Current
      };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
      base.OnExit(e);
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
  }
}