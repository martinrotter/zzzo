using System.Globalization;
using System.Windows;
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

    public App()
    {
      Current = this;

      CultureInfo ci = new CultureInfo("cs-CZ");

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

      // CefSharp.
      Cef.Initialize(new CefSettings
      {
        Locale = CultureInfo.CurrentCulture.IetfLanguageTag
      });

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

      // Theme.
      SetDefaultTheme();
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