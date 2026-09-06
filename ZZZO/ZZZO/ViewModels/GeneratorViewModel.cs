using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CefSharp;
using CefSharp.Wpf;
using ZZZO.Commands;
using ZZZO.Common.Generators;
using ZZZO.Controls;
using static ZZZO.Common.Generators.Generator;
using EnumConverter = ZZZO.Converters.EnumConverter;
namespace ZZZO.ViewModels;

public class GeneratorViewModel : ViewModelBase
{
  public ZzzoCore Core { get; }
  public GeneratorHtml GeneratorHtml { get; } = new();
  public Func<string, TypDokumentu, Task> ZobrazitNahledAsync { get; set; }
  public Action VymazatNahled { get; set; }
  private bool _pracuje;
  private long _revizeNahledu = -1;
  private TypDokumentu _druh = TypDokumentu.Zapis;
  private TypDokumentu _druhNahledu;
  private string _stylNahledu;
  private string _stav = "Vygenerujte náhled dokumentu.";
  public string Stav { get => _stav; private set => SetProperty(ref _stav, value); }
  public bool Pracuje { get => _pracuje; private set { SetProperty(ref _pracuje, value); ObnovitPrikazy(); } }
  public bool CanGenerateOutputs => !Pracuje && GeneratedData != null && _revizeNahledu == Core.Revize && _druhNahledu == SelectedKindOfDocument && _stylNahledu == SelectedHtmlStyle;
  public byte[] GeneratedData { get; private set; }
  public string GeneratedHtml => GeneratedData == null ? null : Encoding.UTF8.GetString(GeneratedData);
  public TypDokumentu SelectedKindOfDocument
  {
    get => _druh;
    set { if (SetProperty(ref _druh, value)) Zneplatnit(); }
  }
  public string SelectedHtmlStyle
  {
    get => Core.Zasedani?.HtmlStyle ?? GeneratorHtml.Styles.FirstOrDefault();
    set { if (Core.Zasedani != null && value != SelectedHtmlStyle) { Core.Zasedani.HtmlStyle = value; OnPropertyChanged(); Zneplatnit(); } }
  }
  public ICommand GenerateDocumentCmd { get; }
  public ICommand ExportHtmlCmd { get; }
  public ICommand ExportPdfCmd { get; }
  public ICommand PrintCmd { get; }

  public GeneratorViewModel(ZzzoCore core)
  {
    Core = core;
    Core.DataZmenena += Zneplatnit;
    Core.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName != nameof(Core.Zasedani)) return;
      GeneratedData = null;
      _revizeNahledu = -1;
      VymazatNahled?.Invoke();
      OnPropertyChanged(nameof(SelectedHtmlStyle));
      Zneplatnit();
    };
    GenerateDocumentCmd = new RelayCommandEmpty(async () => await VygenerovatAsync(), () => !Pracuje && Core.Zasedani != null);
    ExportHtmlCmd = new RelayCommandEmpty(async () => await ExportovatAsync(null, false), () => CanGenerateOutputs);
    ExportPdfCmd = new RelayCommand<ChromiumWebBrowser>(async b => await ExportovatAsync(b, true), b => CanGenerateOutputs && b != null);
    PrintCmd = new RelayCommand<ChromiumWebBrowser>(async b =>
    {
      try { await TinyMceEditor.DokoncitVseAsync(); if (CanGenerateOutputs) b.Print(); else Zneplatnit(); }
      catch (Exception ex) { ZobrazitChybu(ex); }
    }, b => CanGenerateOutputs && b != null);
  }

  private void ObnovitPrikazy() { OnPropertyChanged(nameof(CanGenerateOutputs)); CommandManager.InvalidateRequerySuggested(); }
  private void Zneplatnit()
  {
    Stav = GeneratedData == null ? "Vygenerujte náhled dokumentu." : "Data nebo nastavení se změnila. Přegenerujte náhled před exportem.";
    ObnovitPrikazy();
  }
  private static void ZobrazitChybu(Exception ex) => MessageBox.Show(ex.Message, "Dokument nelze zpracovat", MessageBoxButton.OK, MessageBoxImage.Error);

  public async Task VygenerovatAsync()
  {
    if (Pracuje) return;
    Pracuje = true;
    try
    {
      Stav = "Připravuji dokument…";
      await TinyMceEditor.DokoncitVseAsync();
      Core.AktualizovatValidaci();
      var chyby = Core.Chyby.Where(c => c.Blokuje(SelectedKindOfDocument)).ToList();
      if (chyby.Count > 0)
      {
        Stav = $"Dokument nelze vytvořit: opravte {chyby.Count} chyb ve stavovém řádku dole.";
        return;
      }
      var zasedani = Core.Zasedani;
      var snimek = zasedani.VytvoritSnimek();
      long revize = Core.Revize;
      var druh = SelectedKindOfDocument;
      string styl = SelectedHtmlStyle;
      byte[] data = await GeneratorHtml.Generate(snimek, new Progress<int>(), new GeneratorHtmlParams { HtmlStyle = styl, KindOfDocument = druh });
      if (Core.Zasedani != zasedani) return;
      if (ZobrazitNahledAsync == null) throw new InvalidOperationException("Náhled ještě není připraven.");
      await ZobrazitNahledAsync(Encoding.UTF8.GetString(data), druh);
      if (Core.Zasedani != zasedani) return;
      GeneratedData = data;
      _revizeNahledu = revize;
      _druhNahledu = druh;
      _stylNahledu = styl;
      Stav = revize == Core.Revize && druh == SelectedKindOfDocument && styl == SelectedHtmlStyle
        ? "Náhled je aktuální. Připraveno k exportu." : "Během generování se změnila data. Přegenerujte náhled.";
      OnPropertyChanged(nameof(GeneratedHtml));
    }
    catch (Exception ex) { _revizeNahledu = -1; Stav = "Generování se nezdařilo; předchozí náhled nelze exportovat. " + ex.Message; }
    finally { Pracuje = false; }
  }

  private async Task ExportovatAsync(ChromiumWebBrowser prohlizec, bool pdf)
  {
    try
    {
      await TinyMceEditor.DokoncitVseAsync();
      if (!CanGenerateOutputs) { Zneplatnit(); return; }
      string pripona = pdf ? "pdf" : "html";
      string suffix = EnumConverter.GetEnumDescription(_druhNahledu).ToLower();
      // Dialog exportu nesmí změnit cestu pracovního souboru ani revizi dat.
      string soubor = ZzzoCore.ChooseSaveFile(Core.Zasedani.VytvoritSnimek(), pripona, true, suffix);
      if (string.IsNullOrWhiteSpace(soubor)) return;
      if (!CanGenerateOutputs) { Zneplatnit(); return; }
      Pracuje = true;
      if (!pdf) File.WriteAllBytes(soubor, GeneratedData);
      else
      {
        bool ulozeno = await prohlizec.PrintToPdfAsync(soubor, new PdfPrintSettings
        {
          DisplayHeaderFooter = true, Landscape = false, PrintBackground = true, PreferCssPageSize = true,
          HeaderTemplate = "<div class='text center'></div>", FooterTemplate = GeneratorHtml.GetStyle(_stylNahledu + ".footer")
        });
        if (!ulozeno) throw new IOException("Prohlížeč nepotvrdil uložení PDF.");
      }
      Stav = "Dokument byl exportován.";
    }
    catch (Exception ex) { ZobrazitChybu(ex); }
    finally { Pracuje = false; }
  }
}
