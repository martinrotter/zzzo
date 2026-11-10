using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using CefSharp;
using CefSharp.Wpf;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using HtmlAgilityPack;
using ZZZO.Common;
using ZZZO.Common.API;
using ZZZO.Common.Generators;
using ZZZO.Controls;

namespace ZZZO.Tests;

internal static class TestyEditoru
{
  internal static int Spustit()
  {
    File.WriteAllText(Path.Combine(Program.Vystupy, "editor-error.txt"), "");
    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
    string native = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x86", "native");
    string resources = File.Exists(Path.Combine(AppContext.BaseDirectory, "resources.pak")) ? AppContext.BaseDirectory : native;
    Cef.Initialize(new CefSettings
    {
      BrowserSubprocessPath = Process.GetCurrentProcess().MainModule.FileName,
      Locale = "cs-CZ", ResourcesDirPath = resources, LocalesDirPath = Path.Combine(resources, "locales"),
      RootCachePath = Path.Combine(Program.Vystupy, "editor-cef-" + Guid.NewGuid().ToString("N"))
    }, performDependencyCheck: false, browserProcessHandler: null);
    var editor = new TinyMceEditor();
    editor.ConsoleMessage += (_, e) => File.AppendAllText(Path.Combine(Program.Vystupy, "editor-console.txt"), e.Message + Environment.NewLine);
    var window = new Window { Content = editor, Width = 950, Height = 700, Left = -30000, Top = -30000, Opacity = 0, ShowActivated = false, ShowInTaskbar = false };
    app.Exit += (_, _) => Cef.Shutdown();
    app.DispatcherUnhandledException += (_, e) => { Console.Error.WriteLine(e.Exception); e.Handled = true; app.Shutdown(1); };
    window.Loaded += async (_, _) =>
    {
      int result = 1;
      string custom = null;
      try
      {
        await Pockat(async () => editor.CanExecuteJavascriptInMainFrame && (await editor.EvaluateScriptAsync("!!window.tinymce && !!tinymce.get('tinymce-editor') && tinymce.get('tinymce-editor').initialized")).Result is true);
        var meeting = Program.Ukazka();
        string originalTemplate = new GeneratorDocx().Templates.First();
        meeting.DocxTemplate = originalTemplate;
        editor.Sablona = originalTemplate;
        var entry = Program.PridatBod(meeting, "Ověření editoru a tabulek", RezimBodu.Informativni);
        entry.PrubehHtml = "<p>Text v písmu rozhraní.</p>";
        editor.Polozka = entry;
        await TinyMceEditor.DokoncitVseAsync();
        await OveritKlavesnici(editor);
        await OveritRamecek(editor, window);
        await Js(editor, "tinymce.get('tinymce-editor').setContent('<p>Text v písmu rozhraní.</p>');'ok'");
        string typography = await Js(editor, "JSON.stringify((() => { const e=tinymce.get('tinymce-editor'), s=e.getWin().getComputedStyle(e.getBody().firstElementChild); return {font:s.fontFamily,size:s.fontSize,line:s.lineHeight,after:s.marginBottom}; })())");
        var appearance = Newtonsoft.Json.Linq.JObject.Parse(typography);
        Program.Overit(((string)appearance["font"]).Contains("Segoe UI") && (string)appearance["size"] == "14px" && (string)appearance["line"] == "19.6px", "editor používá čitelné písmo UI Segoe UI 14 px");
        Program.Overit(await Js(editor, "String(tinymce.get('tinymce-editor').options.get('menubar'))") == "false", "editor má pouze základní lištu bez menu");
        editor.Polozka = new Usneseni { TextHtml = "<p>Usnesení.</p>" };
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit((await Js(editor, "tinymce.get('tinymce-editor').getWin().getComputedStyle(tinymce.get('tinymce-editor').getBody()).fontFamily")).Contains("Segoe UI"), "usnesení používá stejné písmo UI jako průběh");
        editor.Polozka = entry;

        custom = "zzzo-editor-test-" + Guid.NewGuid().ToString("N") + ".docx";
        string customPath = Path.Combine(Constants.PathsAndFiles.AppTemplatesFolder, custom);
        File.WriteAllBytes(customPath, GeneratorDocx.ReadTemplate(new GeneratorDocx().Templates.First()));
        using (var package = WordprocessingDocument.Open(customPath, true))
        {
          var normal = package.MainDocumentPart.StyleDefinitionsPart.Styles.Elements<DocumentFormat.OpenXml.Wordprocessing.Style>().Single(s => s.StyleId == "Normal");
          normal.StyleRunProperties.FontSize.Val = "28";
          normal.StyleParagraphProperties.SpacingBetweenLines.Line = "340";
          normal.StyleParagraphProperties.SpacingBetweenLines.After = "240";
        }
        editor.Sablona = custom;
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit(await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'), s=e.getWin().getComputedStyle(e.getBody().firstElementChild); return String(s.fontSize==='14px' && s.lineHeight==='19.6px' && s.marginBottom==='16px'); })()") == "true", "změna šablony zachová písmo UI a převezme pouze odstavcové mezery");
        editor.Sablona = originalTemplate;
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit((await Js(editor, "tinymce.get('tinymce-editor').getWin().getComputedStyle(tinymce.get('tinymce-editor').getBody()).fontSize")) == "14px", "návrat na původní šablonu ponechá písmo UI");
        string formats = await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); e.setContent('<p>Základ</p>'); e.selection.select(e.getBody().firstElementChild,true); e.execCommand('Bold'); e.execCommand('Underline'); e.execCommand('Strikethrough'); return e.getContent(); })()");
        Program.Overit(formats.Contains("<strong>") && formats.Contains("<u>") && formats.Contains("<s>"), "základní tlačítka vytvářejí přenositelné značky místo volného CSS");
        Program.Overit(await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); e.setContent('<p>Původní.</p>'); e.undoManager.clear(); e.undoManager.add(); e.selection.select(e.getBody(),true); e.selection.collapse(false); e.undoManager.transact(()=>e.insertContent('<p>Přidáno.</p>')); e.undoManager.undo(); const undo=!e.getContent().includes('Přidáno'); e.undoManager.redo(); return String(undo && e.getContent().includes('Přidáno')); })()") == "true", "zpět a znovu zachovají obsah po čištění a změně formátování");
        await Js(editor, "tinymce.get('tinymce-editor').setContent('<p>Rozepsaný text před přepnutím.</p>');'ok'");
        var other = new BodProgramu { PrubehHtml = "<p>Jiný bod.</p>" };
        editor.Polozka = other; editor.Polozka = entry;
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit(entry.PrubehHtml.Contains("Rozepsaný") && !other.PrubehHtml.Contains("Rozepsaný"), "změny vzhledu a rychlé přepnutí zachovají rozepsaný obsah u správného bodu");
        entry.PrubehHtml = "<h3>Starší nadpis</h3><p><img style='width:20px;height:30px' src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jM1kAAAAASUVORK5CYII='></p>";
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit(entry.PrubehHtml.Contains("<h3>") && entry.PrubehHtml.Contains("width=\"20\"") && entry.PrubehHtml.Contains("height=\"30\""), "načtení staršího obsahu zachová nadpis i velikost vloženého obrázku");
        string inserted = await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); e.setContent(''); e.execCommand('mceInsertTable',false,{rows:2,columns:2}); const td=e.getBody().querySelector('td'); e.selection.select(td,true); e.selection.collapse(true); e.execCommand('mceTableInsertColAfter'); const count=e.getBody().querySelector('tr').cells.length; e.execCommand('mceTableDeleteCol'); return String(count===3 && e.getBody().querySelector('tr').cells.length===2 && !!e.getBody().querySelector('colgroup')); })()");
        Program.Overit(inserted == "true", "omezené ovládání vloží tabulku a přidá i odstraní sloupec se zachováním mřížky");
        // Resize bars change col styles and emit TableModified. Exercise that
        // contract without injecting mouse gestures into the user's desktop.
        string resizedHtml = await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); [...e.getBody().querySelectorAll('col')].forEach((c,i)=>e.dom.setStyle(c,'width',i?'80%':'20%')); [...e.getBody().querySelectorAll('td')].forEach((c,i)=>c.textContent='Sloupec '+(i+1)); e.dispatch('TableModified',{table:e.getBody().querySelector('table'),structure:false,style:true}); return e.getContent(); })()");
        File.WriteAllText(Path.Combine(Program.Vystupy, "editor-resized.html"), resizedHtml);
        Program.Overit(await Js(editor, "(() => { const c=[...tinymce.get('tinymce-editor').getBody().querySelectorAll('col')].map(c=>c.getBoundingClientRect().width); return String(c.length===2 && Math.abs(c[0]/(c[0]+c[1])-.2)<.01); })()") == "true", "změněné šířky se v editoru skutečně zobrazí v poměru 20/80");

        const string word = "<style>.MsoBold{font-weight:700;color:red;font-family:Calibri;font-size:30pt}@list l0:level1{mso-level-number-format:decimal}@list l0:level2{mso-level-number-format:bullet}</style>" +
          "<p style='margin-top:80pt;font-size:30pt;text-align:center'><span class='MsoBold'>Tučně</span> <i>kurzíva</i> <span style='text-decoration:underline line-through'>vyznačeno</span> H<sub>2</sub>O x<sup>2</sup></p>" +
          "<p style='mso-list:l0 level1 lfo1'><span style='mso-list:Ignore'>3. </span>Třetí položka</p><p style='mso-list:l0 level2 lfo1'><span style='mso-list:Ignore'>· </span>Vnořená položka</p><p style='mso-list:l0 level1 lfo1'><span style='mso-list:Ignore'>4. </span>Čtvrtá položka</p>" +
          "<script>window.zzzoEvil=true</script><p><a href='javascript:alert(1)'>Neaktivní odkaz</a></p>";
        string wordHtml = await Vlozit(editor, word);
        File.WriteAllText(Path.Combine(Program.Vystupy, "editor-word.html"), wordHtml);
        var parsed = new HtmlDocument(); parsed.LoadHtml(wordHtml);
        Program.Overit(parsed.DocumentNode.SelectSingleNode("//strong[contains(.,'Tučně')]") != null && parsed.DocumentNode.SelectSingleNode("//em") != null && parsed.DocumentNode.SelectSingleNode("//u") != null && parsed.DocumentNode.SelectSingleNode("//s") != null && parsed.DocumentNode.SelectSingleNode("//sub") != null && parsed.DocumentNode.SelectSingleNode("//sup") != null, "vložení z Wordu zachová základní formátování a indexy");
        Program.Overit(parsed.DocumentNode.SelectSingleNode("//ol[@start='3']/li/ul/li") != null && !wordHtml.Contains("mso-") && !wordHtml.Contains("MsoBold"), "vložení z Wordu převede číslování i vnoření na skutečné seznamy");
        Program.Overit(!wordHtml.Contains("font-") && !wordHtml.Contains("color") && !wordHtml.Contains("margin") && !wordHtml.Contains("javascript:") && await Js(editor, "typeof window.zzzoEvil") == "undefined", "vložení odstraní cizí sazbu, aktivní obsah a neplatné odkazy");

        const string excel = "<style>.xl65{font-weight:700;font-family:Calibri;color:red;text-align:right}</style><table width='640' style='width:480pt;height:90pt' border='1' cellpadding='8'><colgroup><col width='128'><col style='width:288pt'><col width='128'></colgroup><tbody>" +
          "<tr height='25'><th colspan='3'>Rozpočet</th></tr><tr><td rowspan='2'>Běžné příjmy</td><td class='xl65'>1000 Kč</td><td>20 %</td></tr><tr><td>2000 Kč</td><td>40 %</td></tr></tbody></table>";
        string excelHtml = await Vlozit(editor, excel);
        File.WriteAllText(Path.Combine(Program.Vystupy, "editor-excel.html"), excelHtml);
        Program.Overit(await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'), s=e.getWin().getComputedStyle(e.getBody().querySelector('td')); return String(s.borderTopStyle==='solid' && s.borderTopColor==='rgb(0, 0, 0)'); })()") == "true", "tabulka v editoru používá skutečné černé ohraničení z DOCX šablony");
        parsed.LoadHtml(excelHtml);
        Program.Overit(parsed.DocumentNode.SelectNodes("//colgroup/col")?.Count == 3 && !excelHtml.Contains("height") && !excelHtml.Contains("border=") && !excelHtml.Contains("cellpadding") && !excelHtml.Contains("xl65"), "vložení z Excelu zachová geometrii a sloučené buňky, odstraní dekorace");
        Program.Overit(parsed.DocumentNode.SelectSingleNode("//td[@rowspan='2']") != null && parsed.DocumentNode.SelectSingleNode("//th[@colspan='3']") != null && parsed.DocumentNode.SelectSingleNode("//td/strong[contains(.,'1000')]") != null, "vložení z Excelu zachová obsah a zvýraznění buněk");
        await Js(editor, "tinymce.get('tinymce-editor').setContent(" + Newtonsoft.Json.JsonConvert.SerializeObject(wordHtml + excelHtml + resizedHtml) + ");'ok'");
        await TinyMceEditor.DokoncitVseAsync();
        byte[] docx = await new GeneratorDocx().Generate(meeting, null);
        using (var stream = new MemoryStream(docx))
        using (var package = WordprocessingDocument.Open(stream, false))
        {
          var errors = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2019).Validate(package).ToList();
          foreach (var error in errors) Console.WriteLine(error.Description);
          Program.Overit(errors.Count == 0, "obsah skutečně vložený do editoru vytvoří validní DOCX");
          XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
          var body = XDocument.Load(package.MainDocumentPart.GetStream());
          var tables = body.Descendants(w + "tbl").Where(t => t.Element(w + "tblPr")?.Element(w + "tblStyle")?.Attribute(w + "val")?.Value == "ContentTable").ToArray();
          var table = tables[0];
          var widths = table.Element(w + "tblGrid").Elements().Select(c => (int)c.Attribute(w + "w")).ToArray();
          Program.Overit(widths.Length == 3 && Math.Abs((double)widths[1] / widths.Sum() - .6) < .001 && Math.Abs((double)widths[0] / widths.Sum() - .2) < .001, "poměr sloupců 20/60/20 projde z Excelu přes TinyMCE až do Wordu");
          var resizedWidths = tables[1].Element(w + "tblGrid").Elements().Select(c => (int)c.Attribute(w + "w")).ToArray();
          Program.Overit(resizedWidths.Length == 2 && Math.Abs((double)resizedWidths[0] / resizedWidths.Sum() - .2) < .001, "změna šířky sloupce v editoru se zachová v DOCX");
          Program.Overit(table.Descendants(w + "gridSpan").Any() && table.Descendants(w + "vMerge").Count() == 2 && body.Descendants(w + "strike").Any(), "DOCX zachová sloučení a přeškrtnutí z vyčištěného obsahu");
        }
        File.WriteAllBytes(Path.Combine(Program.Vystupy, "editor-export.docx"), docx);
        File.WriteAllBytes(Path.Combine(Program.Vystupy, "editor-export.pdf"), await new PrevodnikPdf().PrevestAsync(docx));
        await Task.Delay(200);
        var image = new RenderTargetBitmap((int)editor.ActualWidth, (int)editor.ActualHeight, 96, 96, PixelFormats.Pbgra32); image.Render(editor);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using (var file = File.Create(Path.Combine(Program.Vystupy, "editor.png"))) encoder.Save(file);
        Console.WriteLine($"OK: {Program.Pocet} kontrol včetně skutečného TinyMCE mimo obrazovku.");
        result = 0;
      }
      catch (Exception ex) { Console.Error.WriteLine(ex); File.WriteAllText(Path.Combine(Program.Vystupy, "editor-error.txt"), ex.ToString()); }
      finally
      {
        if (custom != null) File.Delete(Path.Combine(Constants.PathsAndFiles.AppTemplatesFolder, custom));
        editor.Dispose(); window.Close(); app.Shutdown(result);
      }
    };
    return app.Run(window);
  }

  private static async Task OveritKlavesnici(TinyMceEditor editor)
  {
    editor.GetBrowserHost().SetFocus(true);
    await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); e.setContent('<p></p>'); e.focus(); e.selection.setCursorLocation(e.getBody().firstElementChild,0); return 'ok'; })()");
    editor.GetBrowserHost().SendKeyEvent(0x100, 0x32, 0x30001);
    Napsat(editor, "ě");
    editor.GetBrowserHost().SendKeyEvent(0x101, 0x32, unchecked((int)0xC0030001));
    await Task.Delay(200);
    Program.Overit(await Js(editor, "tinymce.get('tinymce-editor').getBody().textContent") == "ě", "klávesa 2/ě projde přes skutečnou událost WPF TextInput do TinyMCE");

    const string text = "aěčěěščřžýáíéůúďťň ĚŠČŘŽÝÁÍÉŮÚĎŤŇ";
    foreach (char character in text) Napsat(editor, character.ToString());
    await Task.Delay(200);
    Program.Overit(await Js(editor, "tinymce.get('tinymce-editor').getBody().textContent") == "ě" + text, "rychlé střídání ASCII a českých znaků zachová pořadí, velikost a opakování");
    await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); e.setContent('<p>AB</p>'); e.undoManager.clear(); e.undoManager.add(); const n=e.getBody().firstChild.firstChild, r=e.getDoc().createRange(); r.setStart(n,1); r.setEnd(n,2); e.selection.setRng(r); return 'ok'; })()");
    editor.GetBrowserHost().SendKeyEvent(0x100, 0x32, 0x30001);
    Napsat(editor, "ě");
    editor.GetBrowserHost().SendKeyEvent(0x101, 0x32, unchecked((int)0xC0030001));
    await Task.Delay(200);
    Program.Overit(await Js(editor, "tinymce.get('tinymce-editor').getBody().textContent") == "Aě", "Unicode vstup nahradí vybraný text na správném místě");
    Program.Overit(await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); e.execCommand('Undo'); const ok=e.getBody().textContent==='AB'; e.execCommand('Redo'); return String(ok && e.getBody().textContent==='Aě'); })()") == "true", "Unicode vstup lze vrátit a znovu provést");
  }

  private static void Napsat(TinyMceEditor editor, string text) => editor.RaiseEvent(
    new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, editor, text))
      { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });

  private static async Task OveritRamecek(TinyMceEditor editor, Window window)
  {
    await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'); e.setContent('<p>ZO Svésedlice schvaluje předsedu kontrolního výboru Romana Doležela.</p><p>Ověření české klávesnice: ěščřžýáíé.</p>'); e.focus(); return 'ok'; })()");
    foreach (int height in new[] { 700, 260 })
    {
      window.Height = height;
      await Task.Delay(250);
      string geometry = await Js(editor, "(() => { const e=tinymce.get('tinymce-editor'), a=e.getContainer().querySelector('.tox-edit-area'), r=a.getBoundingClientRect(), c=e.getContainer().getBoundingClientRect(), s=getComputedStyle(a,'::before'); return JSON.stringify({inside:r.left>=0 && r.top>=0 && r.right<=innerWidth && r.bottom<=innerHeight && c.bottom<=innerHeight,inset:s.inset,width:s.borderTopWidth,radius:s.borderBottomLeftRadius,viewport:[innerWidth,innerHeight],area:r.toJSON(),container:c.toJSON()}); })()");
      var bounds = Newtonsoft.Json.Linq.JObject.Parse(geometry);
      if (!(bool)bounds["inside"] || (string)bounds["inset"] != "2px" || (string)bounds["width"] != "1px" || (string)bounds["radius"] != "2px")
        Console.WriteLine("Focus geometry: " + geometry);
      Program.Overit((bool)bounds["inside"] && (string)bounds["inset"] == "2px" && (string)bounds["width"] == "1px" && (string)bounds["radius"] == "2px", $"rámeček je celý uvnitř editoru včetně rohů při výšce {height}");
      if (height == 260)
      {
        var image = new RenderTargetBitmap((int)editor.ActualWidth, (int)editor.ActualHeight, 96, 96, PixelFormats.Pbgra32); image.Render(editor);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var file = File.Create(Path.Combine(Program.Vystupy, "editor-focus.png")); encoder.Save(file);
      }
    }
    window.Height = 700;
    await Task.Delay(250);
  }

  private static async Task<string> Js(TinyMceEditor editor, string code)
  {
    var result = await editor.EvaluateScriptAsync(code);
    if (!result.Success) throw new InvalidOperationException(result.Message);
    return Convert.ToString(result.Result) ?? "";
  }
  private static async Task<string> Vlozit(TinyMceEditor editor, string html) => await Js(editor,
    "(() => { const e=tinymce.get('tinymce-editor'); e.setContent(''); e.execCommand('mceInsertClipboardContent',false,{html:" + Newtonsoft.Json.JsonConvert.SerializeObject(html) + "}); return e.getContent(); })()");
  private static async Task Pockat(Func<Task<bool>> condition)
  {
    for (int attempt = 0; attempt < 150; attempt++) { if (await condition()) return; await Task.Delay(100); }
    throw new TimeoutException("Editor nedokončil inicializaci nebo neodpověděl.");
  }
}
