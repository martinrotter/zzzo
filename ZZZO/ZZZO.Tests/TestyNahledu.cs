using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZZZO.Controls;
using static ZZZO.Common.Generators.Generator;

namespace ZZZO.Tests;

/// <summary>Exercises the real viewer off-screen, without opening the application UI.</summary>
internal static class TestyNahledu
{
  internal static int Spustit()
  {
    File.WriteAllText(Path.Combine(Program.Vystupy, "preview-error.txt"), "");
    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    var preview = new NahledPdf();
    var window = new Window
    {
      Content = preview, Width = 1100, Height = 800, Left = -30000, Top = -30000,
      ShowInTaskbar = false, ShowActivated = false, Opacity = 0
    };
    window.Loaded += async (_, _) =>
    {
      int result = 1;
      try
      {
        byte[] pdf = File.ReadAllBytes(Path.Combine(Program.Vystupy, "zapis.pdf"));
        await preview.ZobrazitAsync(pdf, TypDokumentu.Zapis);
        Program.Overit(await VykresleneAsync(preview), "headless PDF vykreslí první viditelné strany");
        preview.ScrollTo(1400);
        Program.Overit(await VykresleneAsync(preview), "headless PDF vykreslí posunutý náhled");
        double offset = preview.VerticalOffset;
        Program.Overit(offset > 100, "headless PDF má nenulovou pozici");
        for (int i = 0; i < 3; i++)
        {
          await preview.ZobrazitAsync(pdf.ToArray(), TypDokumentu.Zapis);
          Program.Overit(Math.Abs(preview.VerticalOffset - offset) <= 2, "headless opakovaný náhled zachová scroll " + i);
          Program.Overit(await VykresleneAsync(preview), "headless stejný rozsah stran se vykreslí bez dalšího scrollu " + i);
        }
        UlozitSnimek(preview, "preview-obnoveno.png");
        await preview.ZobrazitAsync(File.ReadAllBytes(Path.Combine(Program.Vystupy, "pozvanka.pdf")), TypDokumentu.Pozvanka);
        Program.Overit(preview.VerticalOffset <= 2 && await VykresleneAsync(preview), "headless jiný typ dokumentu začíná na vlastní pozici");
        await preview.ZobrazitAsync(pdf, TypDokumentu.Zapis);
        Program.Overit(Math.Abs(preview.VerticalOffset - offset) <= 2 && await VykresleneAsync(preview), "headless návrat obnoví vykreslené PDF na původní pozici");
        preview.NastavitZoom(2);
        await Task.Delay(400); // Finish the viewer's 200 ms zoom animation.
        preview.Scroll.ScrollToHorizontalOffset(200);
        preview.UpdateLayout();
        double horizontal = preview.HorizontalOffset, vertical = preview.VerticalOffset;
        await preview.ZobrazitAsync(pdf, TypDokumentu.Zapis);
        Program.Overit(horizontal > 100 && Math.Abs(preview.HorizontalOffset - horizontal) <= 2 &&
          Math.Abs(preview.VerticalOffset - vertical) <= 2, "headless přiblížený náhled zachová oba směry scrollu");
        Program.Overit(await VykresleneAsync(preview), "headless přiblížené PDF se po obnově vykreslí bez posunu");
        using var print = preview.VytvoritTisk();
        Program.Overit(print.GetType().Name == "PdfPrintDocument", "headless tisk používá nativní PDFium PrintDocument");
        result = 0;
        Console.WriteLine($"OK: {Program.Pocet} kontrol včetně headless náhledu.");
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
        File.WriteAllText(Path.Combine(Program.Vystupy, "preview-error.txt"), ex.ToString());
        UlozitSnimek(preview, "preview-error.png");
      }
      finally { preview.Vymazat(); window.Close(); app.Shutdown(result); }
    };
    return app.Run(window);
  }

  internal static async Task<bool> VykresleneAsync(NahledPdf preview)
  {
    for (int attempt = 0; attempt < 50; attempt++)
    {
      await preview.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
      var scroll = preview.Scroll;
      int visible = 0;
      bool rendered = true;
      for (int i = 0; i < preview.PageCount; i++)
      {
        if (preview.Prohlizec.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement page) continue;
        double top = page.TransformToAncestor(scroll).Transform(new Point()).Y;
        if (top >= scroll.ViewportHeight || top + page.ActualHeight <= 0) continue;
        visible++;
        rendered &= preview.Prohlizec.Items[i] is System.Windows.Controls.Image { Source: BitmapSource };
      }
      if (visible > 0 && rendered) return true;
      await Task.Delay(40);
    }
    return false;
  }

  private static void UlozitSnimek(FrameworkElement preview, string name)
  {
    var bitmap = new RenderTargetBitmap((int)preview.ActualWidth, (int)preview.ActualHeight, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(preview);
    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var file = File.Create(Path.Combine(Program.Vystupy, name)); encoder.Save(file);
  }
}
