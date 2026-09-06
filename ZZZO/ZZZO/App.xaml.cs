using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CefSharp;
using CefSharp.Wpf;
using MaterialDesignThemes.Wpf;
using ZZZO.Common.API;
using ZZZO.ViewModels;
using ZZZO.Windows;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

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

    internal string AdresarMezipametiProhlizece { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZZZO", "Cef");
    internal string SouborNastaveni { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZZZO", "nastaveni.json");

    public App()
    {
      Current = this;

      CultureInfo ci = new CultureInfo("cs-CZ");

      CultureInfo.CurrentCulture = ci;
      CultureInfo.CurrentUICulture = ci;
      CultureInfo.DefaultThreadCurrentCulture = ci;
      CultureInfo.DefaultThreadCurrentUICulture = ci;
      Thread.CurrentThread.CurrentCulture = ci;
      Thread.CurrentThread.CurrentUICulture = ci;
      FrameworkElement.LanguageProperty.OverrideMetadata(
        typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(ci.IetfLanguageTag)));
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

      // Při publikování leží CEF u EXE; běžné SDK sestavení používá runtimes.
      string nativniAdresar = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x86", "native");
      string zdroje = File.Exists(Path.Combine(AppContext.BaseDirectory, "resources.pak")) ? AppContext.BaseDirectory : nativniAdresar;
      string lokalizace = File.Exists(Path.Combine(AppContext.BaseDirectory, "locales", "cs.pak"))
        ? Path.Combine(AppContext.BaseDirectory, "locales") : Path.Combine(nativniAdresar, "locales");
      Cef.Initialize(
        new CefSettings
        {
          BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName,
          Locale = CultureInfo.CurrentCulture.IetfLanguageTag,
          ResourcesDirPath = zdroje,
          LocalesDirPath = lokalizace,
          RootCachePath = AdresarMezipametiProhlizece
        },
        performDependencyCheck: false,
        browserProcessHandler: null);

      Core.PropertyChanged += (sender, args) =>
      {
        if (args.PropertyName == nameof(ZzzoCore.Zasedani))
        {
          if (Core.Zasedani != null)
          {
            AktualizovatBarvy(Core.Zasedani.LogoObce);

            Core.Zasedani.PropertyChanged += (o, zasArgs) =>
            {
              if (zasArgs.PropertyName == nameof(Zasedani.LogoObce))
              {
                AktualizovatBarvy(Core.Zasedani.LogoObce);
              }
            };
          }
          else
          {
            SetDefaultTheme();
          }
        }
      };

      // Okno vytvoříme až po inicializaci prohlížeče a tématu.
      SetDefaultTheme();
      MainWindow = new MainWindow();
      MainWindow.Show();
    }

    private void SetDefaultTheme()
    {
      AdjustThemeContrastAndColors(
        (Color)FindResource("ThemeColorMain"),
        (Color)FindResource("ThemeColorAccent"));
    }

    private void AktualizovatBarvy(BitmapImage zasedaniLogoObce)
    {
      if (zasedaniLogoObce == null)
      {
        SetDefaultTheme();
        return;
      }

      try
      {
        var clrs = ColorImageSearcher.Get2MostUsedColors(Core.Zasedani.LogoObce).ToArray();

        App.Current.AdjustThemeContrastAndColors(
          clrs[0],
          clrs[1]);
      }
      catch
      {
        //
      }
    }

    public void AdjustThemeContrastAndColors(Color primary = default, Color secondary = default)
    {
      PaletteHelper a = new PaletteHelper();
      Theme b = a.GetTheme() as Theme;

      //IBaseTheme baseTheme = true ? new MaterialDesignDarkTheme() : (IBaseTheme)new MaterialDesignLightTheme();
      //b.SetBaseTheme(baseTheme);
      if (primary != default)
      {
        b.SetPrimaryColor(primary);
        Resources["PozadiNajetiPolozky"] = new SolidColorBrush(Color.FromArgb(0x18, primary.R, primary.G, primary.B));
        Resources["PozadiVybranePolozky"] = new SolidColorBrush(Color.FromArgb(0x38, primary.R, primary.G, primary.B));
        Resources["OkrajVybranePolozky"] = new SolidColorBrush(Color.FromArgb(0xCC, primary.R, primary.G, primary.B));
      }

      if (secondary != default)
      {
        b.SetSecondaryColor(secondary);
      }

      b.ColorAdjustment = new ColorAdjustment
      {
        Colors = ColorSelection.All,
        Contrast = Contrast.Medium,
        DesiredContrastRatio = 2.0f
      };

      // Vynutíme barvu pozadí.
      b.Background = new ColorReference(ThemeColorReference.None, Colors.White);

      a.SetTheme(b);
    }

    #endregion
  }
}
