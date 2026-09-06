using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json.Linq;
using static ZZZO.Common.Generators.Generator;
namespace ZZZO.Controls;

/// <summary>Pozice se pamatuje zvlášť pro pozvánku a zápis; změna zasedání ji vynuluje.</summary>
public sealed class PoziceNahledu
{
  private readonly ChromiumWebBrowser _prohlizec;
  private readonly Dictionary<TypDokumentu, JObject> _pozice = new();
  private TypDokumentu? _zobrazenyDruh;
  private int _generace;
  public PoziceNahledu(ChromiumWebBrowser prohlizec) => _prohlizec = prohlizec;
  public void Vymazat()
  {
    _generace++;
    _pozice.Clear();
    _zobrazenyDruh = null;
    if (_prohlizec.IsBrowserInitialized) _prohlizec.LoadHtml("<html><body></body></html>");
  }

  public async Task ZobrazitAsync(string html, TypDokumentu druh)
  {
    int generace = ++_generace;
    if (_zobrazenyDruh.HasValue && _prohlizec.CanExecuteJavascriptInMainFrame)
    {
      var odpoved = await _prohlizec.EvaluateScriptAsync(@"JSON.stringify((() => {
        const prvky = [...document.querySelectorAll('[id]')];
        const kotva = prvky.filter(e => e.getBoundingClientRect().top <= 8).pop() || prvky[0];
        return { x: scrollX, y: scrollY, id: kotva ? kotva.id : '', posun: kotva ? kotva.getBoundingClientRect().top : 0 };
      })())");
      if (odpoved.Success && odpoved.Result is string json && generace == _generace)
        _pozice[_zobrazenyDruh.Value] = JObject.Parse(json);
    }
    if (generace != _generace) return;
    var dokonceno = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    string adresa = "https://nahled.zzzo.invalid/" + Guid.NewGuid().ToString("N");
    EventHandler<FrameLoadEndEventArgs> nacteno = (_, e) =>
    {
      if (e.Frame.IsMain && e.Url.StartsWith(adresa, StringComparison.OrdinalIgnoreCase)) dokonceno.TrySetResult(true);
    };
    _prohlizec.FrameLoadEnd += nacteno;
    try
    {
      _prohlizec.LoadHtml(html, adresa);
      await dokonceno.Task.WaitAsync(TimeSpan.FromSeconds(30));
      if (generace != _generace) return;
      using var ramec = _prohlizec.GetMainFrame();
      // Fonty, obrázky a dvě vykreslení musejí doběhnout před obnovou pozice i exportem.
      var pripraven = await ramec.EvaluateScriptAsPromiseAsync(@"
        return Promise.race([
          Promise.all([document.fonts.ready, ...[...document.images].map(img => img.complete ? Promise.resolve() :
            new Promise(resolve => { img.onload = resolve; img.onerror = resolve; }))]),
          new Promise(resolve => setTimeout(resolve, 8000))
        ]).then(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve(true)))));");
      if (!pripraven.Success) throw new InvalidOperationException(pripraven.Message);
      if (generace != _generace) return;
      var p = _pozice.GetValueOrDefault(druh) ?? new JObject { ["x"] = 0, ["y"] = 0, ["id"] = "", ["posun"] = 0 };
      string jsonPozice = p.ToString(Newtonsoft.Json.Formatting.None);
      var obnova = await ramec.EvaluateScriptAsync(@"(() => {
        const p = " + jsonPozice + @";
        const kotva = document.getElementById(p.id);
        const y = kotva ? kotva.getBoundingClientRect().top + scrollY - p.posun : p.y;
        scrollTo(p.x, Math.max(0, Math.min(y, document.documentElement.scrollHeight - innerHeight)));
      })()");
      if (!obnova.Success) throw new InvalidOperationException(obnova.Message);
      _zobrazenyDruh = druh;
    }
    finally { _prohlizec.FrameLoadEnd -= nacteno; }
  }
}
