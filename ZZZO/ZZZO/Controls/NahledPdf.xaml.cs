using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PdfiumViewer.Core;
using PdfiumViewer.Enums;
using static ZZZO.Common.Generators.Generator;

namespace ZZZO.Controls;

/// <summary>A thin host for PdfiumViewer: the library owns PDF rendering, selection and printing.</summary>
public partial class NahledPdf : UserControl
{
  private const double SkutecneMeritko = 96.0 / 72;
  private readonly Dictionary<TypDokumentu, (double Vertical, double Horizontal)> _positions = new();
  private TypDokumentu? _kind;
  private int _generace;
  public byte[] PdfData { get; private set; }
  public int PageCount => Prohlizec.Document?.PageCount ?? 0;
  public double VerticalOffset => Scroll?.VerticalOffset ?? 0;
  public double HorizontalOffset => Scroll?.HorizontalOffset ?? 0;
  internal ScrollViewer Scroll => Prohlizec.Template?.FindName("PART_Scroll", Prohlizec) as ScrollViewer;

  public NahledPdf()
  {
    InitializeComponent();
    Loaded += (_, _) => Prohlizec.Dpi = (int)Math.Round(VisualTreeHelper.GetDpi(this).PixelsPerInchX);
    PreviewKeyDown += (_, e) =>
    {
      if (Keyboard.Modifiers != ModifierKeys.Control) return;
      if (e.Key is Key.Add or Key.OemPlus) NastavitZoom(Zoom * 1.1);
      else if (e.Key is Key.Subtract or Key.OemMinus) NastavitZoom(Zoom / 1.1);
      else if (e.Key is Key.NumPad0 or Key.D0) NastavitZoom(1);
      else return;
      e.Handled = true;
    };
  }

  protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
  {
    base.OnDpiChanged(oldDpi, newDpi);
    if (Prohlizec != null) Prohlizec.Dpi = (int)Math.Round(newDpi.PixelsPerInchX);
  }

  public async Task ZobrazitAsync(byte[] pdf, TypDokumentu kind)
  {
    int generation = ++_generace;
    if (_kind.HasValue) _positions[_kind.Value] = (VerticalOffset, HorizontalOffset);
    // Finish pending viewer renders before the library disposes the previous PDF.
    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
    if (generation != _generace) return;
    var input = new MemoryStream(pdf, false);
    PdfDocument document;
    try { document = PdfDocument.Load(input); }
    catch { input.Dispose(); throw; }
    // Complete an empty layout before replacing an equally sized PDF. Otherwise
    // PdfiumViewer resets its render range but receives no ScrollChanged event
    // to restore it, leaving the viewport gray until the user moves the scroll.
    Prohlizec.Document = null;
    UpdateLayout();
    Prohlizec.Document = document;
    PdfData = pdf; _kind = kind;
    Prohlizec.ApplyTemplate(); UpdateLayout();
    if (Scroll is ScrollViewer scroll)
    {
      scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
      scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
      scroll.Background = Brushes.DimGray;
    }
    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
    if (generation != _generace) return;
    var position = _positions.GetValueOrDefault(kind);
    Scroll?.ScrollToVerticalOffset(position.Vertical);
    Scroll?.ScrollToHorizontalOffset(position.Horizontal);
    UpdateLayout();
    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
  }

  public void Vymazat()
  {
    _generace++;
    Prohlizec.Document = null; PdfData = null; _kind = null;
    _positions.Clear();
  }

  public void ScrollTo(double offset) { Scroll?.ScrollToVerticalOffset(offset); UpdateLayout(); }
  public double Zoom => Prohlizec.Zoom / SkutecneMeritko;
  public void NastavitZoom(double zoom) { Prohlizec.FitWidth = false; Prohlizec.Zoom = zoom * SkutecneMeritko; }
  private void Kopirovat(object sender, RoutedEventArgs e)
  {
    string text = Prohlizec.Document?.GetSelectionText();
    if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
  }

  public void Tisknout()
  {
    if (Prohlizec.Document == null) return;
    var dialog = new PrintDialog { UserPageRangeEnabled = true, MinPage = 1, MaxPage = (uint)PageCount };
    if (dialog.ShowDialog() != true) return;
    using var print = VytvoritTisk();
    print.PrinterSettings.PrinterName = dialog.PrintQueue.FullName;
    print.PrinterSettings.Copies = (short)(dialog.PrintTicket.CopyCount ?? 1);
    if (dialog.PageRangeSelection == PageRangeSelection.UserPages)
    { print.PrinterSettings.FromPage = dialog.PageRange.PageFrom; print.PrinterSettings.ToPage = dialog.PageRange.PageTo; }
    print.Print();
  }
  internal PrintDocument VytvoritTisk()
  {
    var print = Prohlizec.Document.CreatePrintDocument(PdfPrintMode.ShrinkToMargin);
    print.DocumentName = "ZZZO";
    print.PrintController = new StandardPrintController();
    return print;
  }
}
