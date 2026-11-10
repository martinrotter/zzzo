using System.IO;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json.Linq;
using ZZZO.Common;
using ZZZO.Common.API;
using ZZZO.Common.Generators;

namespace ZZZO.Controls;

public class TinyMceEditor : ChromiumWebBrowser
{
  private static readonly List<WeakReference<TinyMceEditor>> Editory = new();
  public static readonly DependencyProperty PolozkaProperty = DependencyProperty.Register(
    nameof(Polozka), typeof(object), typeof(TinyMceEditor),
    new PropertyMetadata(null, (d, e) => ((TinyMceEditor)d).ZmenitPolozku(e.OldValue, e.NewValue)));
  public object Polozka { get => GetValue(PolozkaProperty); set => SetValue(PolozkaProperty, value); }
  public static readonly DependencyProperty SablonaProperty = DependencyProperty.Register(nameof(Sablona), typeof(string), typeof(TinyMceEditor),
    new PropertyMetadata(null, (d, _) => { var editor = (TinyMceEditor)d; editor._fronta = editor.ObnovitVzhledAsync(editor._fronta); }));
  public string Sablona { get => (string)GetValue(SablonaProperty); set => SetValue(SablonaProperty, value); }

  private readonly TaskCompletionSource<bool> _pripraven = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private readonly Dictionary<int, WeakReference<object>> _polozky = new();
  private readonly Dictionary<int, long> _poradiZprav = new();
  private Task _fronta = Task.CompletedTask;
  private int _verze;
  private Exception _chyba;
  private bool _zapisuje;
  private string _otiskVzhledu;

  public TinyMceEditor()
  {
    Editory.Add(new(this));
    Address = Path.Combine(Constants.PathsAndFiles.AppTinyMceFolder, "editor.html");
    JavascriptMessageReceived += PrijmoutZpravu;
  }

  protected override void OnPreviewTextInput(TextCompositionEventArgs e)
  {
    // The default WPF -> WM_CHAR path drops U+011B (ě) in Chromium.
    // Commit completed Unicode text through CEF's native text-input API;
    // keep key presses/shortcuts and the dedicated IME handler intact.
    if (!e.Handled && e.Text.Any(c => c > 127) && IsBrowserInitialized &&
        WpfKeyboardHandler is not CefSharp.Wpf.Experimental.WpfImeKeyboardHandler)
    {
      // Explicit invalid range means the current selection. A null range in
      // CefSharp becomes (0,0), which inserts at the start of the document.
      this.GetBrowserHost().ImeCommitText(e.Text, new CefSharp.Structs.Range(-1, -1), 0);
      e.Handled = true;
    }
    base.OnPreviewTextInput(e);
  }

  private void PrijmoutZpravu(object sender, JavascriptMessageReceivedEventArgs e)
  {
    if (e.Message is not string json) return;
    Dispatcher.BeginInvoke(new Action(() =>
    {
      try
      {
        var zprava = JObject.Parse(json);
        if ((string)zprava["druh"] == "pripraven") { _pripraven.TrySetResult(true); return; }
        UlozitObsah(zprava);
      }
      catch (Exception ex) { _chyba = ex; }
    }));
  }

  private void UlozitObsah(JObject zprava)
  {
    int verze = (int?)zprava["verze"] ?? -1;
    if (!_polozky.TryGetValue(verze, out var odkaz) || !odkaz.TryGetTarget(out var polozka)) return;
    long poradi = (long?)zprava["poradi"] ?? 0;
    if (_poradiZprav.TryGetValue(verze, out long posledni) && poradi <= posledni) return;
    _poradiZprav[verze] = poradi;
    string html = (string)zprava["html"] ?? "";
    _zapisuje = true;
    try
    {
      if (polozka is BodProgramu bod) bod.PrubehHtml = html;
      else if (polozka is Usneseni usneseni) usneseni.TextHtml = html;
    }
    finally { _zapisuje = false; }
  }

  private void ZmenitPolozku(object stara, object nova)
  {
    if (stara is INotifyPropertyChanged s) PropertyChangedEventManager.RemoveHandler(s, ZmenitObsah, "");
    if (nova is INotifyPropertyChanged n) PropertyChangedEventManager.AddHandler(n, ZmenitObsah, "");
    _fronta = PrepnoutAsync(_fronta, nova);
  }
  private void ZmenitObsah(object sender, PropertyChangedEventArgs e)
  {
    if (!_zapisuje && e.PropertyName is nameof(BodProgramu.PrubehHtml) or nameof(Usneseni.TextHtml))
      _fronta = PrepnoutAsync(_fronta, Polozka, false);
  }

  private async Task PrepnoutAsync(Task predchozi, object polozka, bool ulozit = true)
  {
    try
    {
      await predchozi;
      await _pripraven.Task;
      if (ulozit) await PrecistObsahAsync();
      int verze = ++_verze;
      if (polozka != null) _polozky[verze] = new(polozka);
      string html = polozka switch { BodProgramu b => b.PrubehHtml, Usneseni u => u.TextHtml, _ => "" };
      await NastavitVzhledAsync(polozka);
      var vysledek = await this.EvaluateScriptAsync("setEditorContent(" + Newtonsoft.Json.JsonConvert.SerializeObject(html) + "," + verze + ");");
      if (!vysledek.Success) throw new InvalidOperationException(vysledek.Message);
      foreach (int stara in _polozky.Keys.Where(v => v < verze - 32).ToArray()) { _polozky.Remove(stara); _poradiZprav.Remove(stara); }
    }
    catch (Exception ex) { _chyba = ex; }
  }

  private async Task ObnovitVzhledAsync(Task previous)
  {
    try { await previous; await _pripraven.Task; await NastavitVzhledAsync(Polozka); }
    catch (Exception ex) { _chyba = ex; }
  }

  private async Task NastavitVzhledAsync(object polozka)
  {
    string css, fingerprint;
    try
    {
      string template = Sablona ?? new GeneratorDocx().Templates.FirstOrDefault();
      if (template == null || template != Path.GetFileName(template)) return;
      var file = new FileInfo(Path.Combine(Constants.PathsAndFiles.AppTemplatesFolder, template));
      string style = polozka is Usneseni ? "Resolution" : "Normal";
      fingerprint = $"{file.FullName}|{file.LastWriteTimeUtc.Ticks}|{file.Length}|{style}";
      if (_otiskVzhledu == fingerprint) return;
      css = VzhledEditoruDocx.VytvoritCss(GeneratorDocx.ReadTemplate(template), style);
    }
    // A missing/corrupt presentation template must not prevent saving typed text.
    catch (Exception ex) when (ex is IOException or InvalidOperationException or System.Xml.XmlException or FileFormatException or ArgumentException) { return; }
    var result = await this.EvaluateScriptAsync("setEditorAppearance(" + Newtonsoft.Json.JsonConvert.SerializeObject(css) + ");");
    if (!result.Success) throw new InvalidOperationException(result.Message);
    _otiskVzhledu = fingerprint;
  }

  private async Task PrecistObsahAsync()
  {
    if (_verze == 0) return;
    var odpoved = await this.EvaluateScriptAsync("getEditorContent()");
    if (!odpoved.Success || odpoved.Result is not string json)
      throw new InvalidOperationException("Editor nevrátil aktuální obsah. " + odpoved.Message);
    UlozitObsah(JObject.Parse(json));
  }

  public static async Task DokoncitVseAsync()
  {
    await Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
    foreach (var odkaz in Editory.ToArray())
    {
      if (!odkaz.TryGetTarget(out var editor) || editor.IsDisposed) { Editory.Remove(odkaz); continue; }
      // Neotevřená záložka dosud nemohla obsahovat rozepsaný text.
      if (!editor.IsBrowserInitialized) continue;
      Task fronta;
      do
      {
        fronta = editor._fronta;
        await fronta.WaitAsync(TimeSpan.FromSeconds(20));
        if (editor._chyba != null) throw new InvalidOperationException("Obsah HTML editoru nelze bezpečně převzít.", editor._chyba);
        await editor.PrecistObsahAsync();
      } while (fronta != editor._fronta);
    }
  }
}
