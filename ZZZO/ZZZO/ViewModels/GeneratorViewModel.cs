using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.Generators;
using ZZZO.Controls;
using static ZZZO.Common.Generators.Generator;
using EnumConverter = ZZZO.Converters.EnumConverter;
namespace ZZZO.ViewModels;

public class GeneratorViewModel : ViewModelBase
{
  public ZzzoCore Core { get; }
  public GeneratorDocx GeneratorDocx { get; } = new();
  public Func<byte[], TypDokumentu, Task> ZobrazitNahledAsync { get; set; }
  public Action VymazatNahled { get; set; }
  public Action Tisknout { get; set; }
  private bool _pracuje;
  private long _revizeNahledu = -1;
  private TypDokumentu _druh = TypDokumentu.Zapis;
  private TypDokumentu _druhNahledu;
  private string _sablonaNahledu;
  private byte[] _otiskSablony;
  private string _stav = "Vygenerujte dokument ze šablony DOCX.";
  public string Stav { get => _stav; private set => SetProperty(ref _stav, value); }
  public bool Pracuje { get => _pracuje; private set { SetProperty(ref _pracuje, value); ObnovitPrikazy(); } }
  public bool CanGenerateOutputs => !Pracuje && GeneratedData != null && _revizeNahledu == Core.Revize &&
    _druhNahledu == SelectedKindOfDocument && _sablonaNahledu == SelectedTemplate && SablonaJeAktualni();
  public bool CanExportPdf => CanGenerateOutputs && GeneratedPdf != null;
  public byte[] GeneratedData { get; private set; }
  public byte[] GeneratedPdf { get; private set; }
  public TypDokumentu SelectedKindOfDocument
  {
    get => _druh;
    set { if (SetProperty(ref _druh, value)) Zneplatnit(); }
  }
  public string SelectedTemplate
  {
    get => Core.Zasedani?.DocxTemplate ?? GeneratorDocx.Templates.FirstOrDefault();
    set { if (Core.Zasedani != null && value != SelectedTemplate) { Core.Zasedani.DocxTemplate = value; OnPropertyChanged(); Zneplatnit(); } }
  }
  public ICommand GenerateDocumentCmd { get; }
  public ICommand ExportDocxCmd { get; }
  public ICommand ExportPdfCmd { get; }
  public ICommand PrintCmd { get; }

  public GeneratorViewModel(ZzzoCore core)
  {
    Core = core;
    Core.DataZmenena += Zneplatnit;
    Core.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName != nameof(Core.Zasedani)) return;
      GeneratedData = GeneratedPdf = null;
      _revizeNahledu = -1;
      VymazatNahled?.Invoke();
      OnPropertyChanged(nameof(SelectedTemplate));
      Zneplatnit();
    };
    GenerateDocumentCmd = new RelayCommandEmpty(async () => await VygenerovatAsync(), () => !Pracuje && Core.Zasedani != null);
    ExportDocxCmd = new RelayCommandEmpty(async () => await ExportovatAsync(false), () => CanGenerateOutputs);
    ExportPdfCmd = new RelayCommandEmpty(async () => await ExportovatAsync(true), () => CanExportPdf);
    PrintCmd = new RelayCommandEmpty(async () =>
    {
      try { await TinyMceEditor.DokoncitVseAsync(); if (CanExportPdf) Tisknout?.Invoke(); else Zneplatnit(); }
      catch (Exception ex) { ZobrazitChybu(ex); }
    }, () => CanExportPdf && Tisknout != null);
  }

  private bool SablonaJeAktualni()
  {
    try { return _otiskSablony != null && SHA256.HashData(GeneratorDocx.ReadTemplate(SelectedTemplate)).SequenceEqual(_otiskSablony); }
    catch (IOException) { return false; } catch (UnauthorizedAccessException) { return false; }
  }
  private void ObnovitPrikazy()
  {
    OnPropertyChanged(nameof(CanGenerateOutputs)); OnPropertyChanged(nameof(CanExportPdf)); CommandManager.InvalidateRequerySuggested();
  }
  private void Zneplatnit()
  {
    Stav = GeneratedData == null ? "Vygenerujte dokument ze šablony DOCX." : "Data nebo nastavení se změnila. Přegenerujte dokument před exportem.";
    ObnovitPrikazy();
  }
  private static void ZobrazitChybu(Exception ex) => MessageBox.Show(ex.Message, "Dokument nelze zpracovat", MessageBoxButton.OK, MessageBoxImage.Error);

  public async Task VygenerovatAsync()
  {
    if (Pracuje || Core.Zasedani == null) return;
    Pracuje = true;
    _revizeNahledu = -1;
    try
    {
      Stav = "Připravuji DOCX ze šablony…";
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
      string sablona = SelectedTemplate;
      byte[] zdrojSablony = GeneratorDocx.ReadTemplate(sablona);
      byte[] data = await GeneratorDocx.Generate(snimek, new Progress<int>(), new GeneratorDocxParams { TemplateData = zdrojSablony, KindOfDocument = druh });
      byte[] pdf = null;
      string chybaPrevodu = null;
      var prevodnik = new PrevodnikPdf();
      Stav = "Převádím DOCX do PDF pro náhled…";
      try { pdf = await prevodnik.PrevestAsync(data); }
      catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception) { chybaPrevodu = ex.Message; }
      if (Core.Zasedani != zasedani) return;
      GeneratedData = data; GeneratedPdf = pdf;
      _revizeNahledu = revize; _druhNahledu = druh; _sablonaNahledu = sablona; _otiskSablony = SHA256.HashData(zdrojSablony);
      if (pdf != null && ZobrazitNahledAsync != null)
      {
        try { await ZobrazitNahledAsync(pdf, druh); }
        catch (Exception ex) { VymazatNahled?.Invoke(); chybaPrevodu = "PDF vzniklo, náhled se nezdařil: " + ex.Message; }
      }
      else VymazatNahled?.Invoke();
      if (Core.Zasedani != zasedani) return;
      bool aktualni = revize == Core.Revize && druh == SelectedKindOfDocument && sablona == SelectedTemplate && SablonaJeAktualni();
      Stav = !aktualni ? "Během generování se změnila data nebo šablona. Přegenerujte dokument."
        : chybaPrevodu != null ? "DOCX je připraven k exportu. " + chybaPrevodu
        : $"DOCX i PDF jsou aktuální. PDF připravil {prevodnik.PouzityPrevodnik}.";
    }
    catch (Exception ex) { _revizeNahledu = -1; Stav = "Generování se nezdařilo; předchozí dokument nelze exportovat. " + ex.Message; }
    finally { Pracuje = false; }
  }

  private async Task ExportovatAsync(bool pdf)
  {
    try
    {
      await TinyMceEditor.DokoncitVseAsync();
      if (pdf ? !CanExportPdf : !CanGenerateOutputs) { Zneplatnit(); return; }
      string suffix = EnumConverter.GetEnumDescription(_druhNahledu).ToLower();
      string soubor = ZzzoCore.ChooseSaveFile(Core.Zasedani.VytvoritSnimek(), pdf ? "pdf" : "docx", true, suffix);
      if (string.IsNullOrWhiteSpace(soubor)) return;
      if (pdf ? !CanExportPdf : !CanGenerateOutputs) { Zneplatnit(); return; }
      Pracuje = true;
      await File.WriteAllBytesAsync(soubor, pdf ? GeneratedPdf : GeneratedData);
      Stav = "Dokument byl exportován.";
    }
    catch (Exception ex) { ZobrazitChybu(ex); }
    finally { Pracuje = false; }
  }
}
