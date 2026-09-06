using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CefSharp;
using ZZZO.Common.API;
using ZZZO.Controls;
using ZZZO.ViewModels;
using ZZZO.Windows;
using static ZZZO.Common.Generators.Generator;
namespace ZZZO.Tests;

internal static class TestyGui
{
  public static int Spustit()
  {
    int kod = 1;
    Console.WriteLine("GUI: vytvářím aplikaci");
    var app = new App { AdresarMezipametiProhlizece = Path.Combine(Program.Vystupy, "cef-" + Guid.NewGuid().ToString("N")) };
    app.DispatcherUnhandledException += (_, e) => { Console.Error.WriteLine(e.Exception); File.WriteAllText(Path.Combine(Program.Vystupy, "startup-error.txt"), e.Exception.ToString()); e.Handled = true; app.Shutdown(1); };
    app.InitializeComponent();
    var vazby = new ZachytitVazby();
    PresentationTraceSources.DataBindingSource.Listeners.Add(vazby);
    app.Startup += (_, _) => app.Dispatcher.BeginInvoke(new Action(async () =>
    {
      try
      {
        Console.WriteLine("GUI: čekám na okno");
        await PockatAsync(() => Task.FromResult(app.MainWindow is MainWindow));
        var okno = (MainWindow)app.MainWindow;
        okno.ShowInTaskbar = false; okno.Left = -20000; okno.Top = 0;
        var zas = Program.Ukazka();
        var bod = Program.PridatBod(zas, "Test editoru", RezimBodu.SUsnesenimi);
        bod.PrubehHtml = "<p>Původní průběh.</p>";
        bod.Usneseni[0].TextHtml = "<p>První usnesení.</p>";
        bod.PridatUsneseni(zas.Zastupitele).TextHtml = "<p>Druhé usnesení.</p>";
        app.Core.Zasedani = zas;
        okno.Show();
        okno.TcZasedani.SelectedIndex = 1;
        var program = (ProgramViewModel)okno.UcProgram.DataContext;
        program.ChosenProgramEntry = bod;
        var editor = okno.UcProgram.UcProgramEntry.EditorBodu;
        await PockatAsync(async () => editor.CanExecuteJavascriptInMainFrame && (await editor.EvaluateScriptAsync("typeof getEditorContent === 'function' && !!tinymce.get('tinymce-editor') && tinymce.get('tinymce-editor').initialized")).Result is true);
        await TinyMceEditor.DokoncitVseAsync();
        await editor.EvaluateScriptAsync("tinymce.get('tinymce-editor').setContent('<p>Rozepsaný průběh.</p>');");
        // Nečekáme na ztrátu fokusu ani na časovač editoru.
        program.ChosenProgramEntry = zas.Program.BodyProgramu[0];
        program.ChosenProgramEntry = bod;
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit(ZZZO.Common.HtmlObsah.ProstyText(bod.PrubehHtml).Contains("Rozepsaný"), "GUI rychlé přepnutí zachová průběh");
        Program.Overit(!zas.Program.BodyProgramu[0].PrubehHtml.Contains("Rozepsaný"), "GUI text nepřeskočil do jiného bodu");

        program.EntryViewModel.VybranaZalozka = 1;
        await Task.Delay(300);
        var editorUsneseni = Najit<TinyMceEditor>(okno.UcProgram).First(e => e != editor);
        await PockatAsync(async () => editorUsneseni.CanExecuteJavascriptInMainFrame && (await editorUsneseni.EvaluateScriptAsync("!!tinymce.get('tinymce-editor') && tinymce.get('tinymce-editor').initialized")).Result is true);
        await TinyMceEditor.DokoncitVseAsync();
        await editorUsneseni.EvaluateScriptAsync("tinymce.get('tinymce-editor').setContent('<p>Upravené <strong>první</strong> usnesení.</p>');");
        program.EntryViewModel.ChosenUsneseni = bod.Usneseni[1];
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit(bod.Usneseni[0].TextHtml.Contains("<strong>první</strong>"), "GUI HTML prvního usnesení uloženo při přepnutí");
        Program.Overit(bod.Usneseni[1].TextHtml.Contains("Druhé"), "GUI druhé usnesení nepřepsáno");
        await editorUsneseni.EvaluateScriptAsync("tinymce.get('tinymce-editor').setContent('<p>Poslední stisk klávesy.</p>');");
        await TinyMceEditor.DokoncitVseAsync();
        Program.Overit(bod.Usneseni[1].TextHtml.Contains("Poslední"), "GUI flush před uložením");

        await OveritAkceProgramuAsync(okno);
        UlozitSnimek(okno, "program.png");
        okno.TcZasedani.SelectedIndex = 0;
        await Task.Delay(100);
        await OveritSeznamZastupiteluAsync(okno);
        UlozitSnimek(okno, "zakladni-udaje.png");
        okno.TcZasedani.SelectedIndex = 2;
        var generator = (GeneratorViewModel)okno.UcGenerator.DataContext;
        var prohlizec = okno.UcGenerator.WebBrowser;
        await PockatAsync(() => Task.FromResult(prohlizec.IsBrowserInitialized));
        bod.PrubehHtml = string.Concat(Enumerable.Repeat("<p>Dlouhý průběh pro kontrolu pozice náhledu.</p>", 70));
        await generator.VygenerovatAsync();
        Program.Overit(generator.CanGenerateOutputs, "GUI aktuální náhled lze exportovat: " + generator.Stav);
        await prohlizec.EvaluateScriptAsync("scrollTo(0, 1400);");
        double pred = Convert.ToDouble((await prohlizec.EvaluateScriptAsync("scrollY")).Result);
        await generator.VygenerovatAsync();
        Program.Overit(generator.CanGenerateOutputs, "GUI opakované generování: " + generator.Stav);
        double po = Convert.ToDouble((await prohlizec.EvaluateScriptAsync("scrollY")).Result);
        Program.Overit(pred > 100 && Math.Abs(pred - po) <= 2, "GUI scroll zůstává při přegenerování");
        generator.SelectedKindOfDocument = TypDokumentu.Pozvanka;
        Program.Overit(!generator.CanGenerateOutputs, "GUI změna typu zablokuje starý export");
        await generator.VygenerovatAsync();
        generator.SelectedKindOfDocument = TypDokumentu.Zapis;
        await generator.VygenerovatAsync();
        po = Convert.ToDouble((await prohlizec.EvaluateScriptAsync("scrollY")).Result);
        Program.Overit(Math.Abs(pred - po) <= 2, "GUI samostatná pozice pozvánky a zápisu");
        bool pdf = await prohlizec.PrintToPdfAsync(Path.Combine(Program.Vystupy, "zapis.pdf"), new PdfPrintSettings
        {
          DisplayHeaderFooter = true, PrintBackground = true, PreferCssPageSize = true,
          HeaderTemplate = "<div class='text center'></div>",
          FooterTemplate = ZZZO.Common.Generators.GeneratorHtml.GetStyle("svésedlice.css.footer")
        });
        Program.Overit(pdf, "GUI skutečný PDF export");
        bod.Nadpis += " změna";
        Program.Overit(!generator.CanGenerateOutputs, "GUI změna dat zablokuje starý export");
        // Hierarchie se přesouvá včetně podbodů; chybějící usnesení je vidět i na řádku rodiče.
        var rodic = zas.Program.BodyProgramu.First(b => b.JeBezny && b != bod);
        program.ChosenProgramEntry = rodic;
        program.PridatPodbodCmd.Execute(null);
        var podbod = program.ChosenProgramEntry;
        Program.Overit(podbod.Nadpis == "Nový podbod", "GUI nový podbod má výchozí název");
        podbod.Nadpis = "Testovaný podbod";
        Program.Overit(zas.Program.RodicBodu(podbod) == rodic, "GUI přidání skutečného podbodu");
        program.ZmenitNaHlavniCmd.Execute(null);
        Program.Overit(zas.Program.BodyProgramu.Contains(podbod), "GUI vysunutí podbodu");
        program.ZmenitNaPodbodCmd.Execute(null);
        Program.Overit(zas.Program.RodicBodu(podbod) == rodic, "GUI zanoření podbodu");
        podbod.PridatUsneseni(zas.Zastupitele);
        var prevodnik = new ZZZO.Converters.ValidaceConverter();
        Program.Overit((string)prevodnik.Convert(new object[] { rodic, app.Core.Chyby }, typeof(string), "Ikona", System.Globalization.CultureInfo.CurrentCulture) == "⚠", "GUI chyba usnesení viditelná u rodiče");
        var chybaUsneseni = app.Core.Chyby.First(c => c.Kod == "text-usneseni" && c.PolozkaId == podbod.Usneseni[0].Id);
        app.Core.PrejitNaChybuCmd.Execute(chybaUsneseni);
        Program.Overit(program.ChosenProgramEntry == podbod && program.EntryViewModel.ChosenUsneseni == podbod.Usneseni[0] && program.EntryViewModel.VybranaZalozka == 1, "GUI navigace z validace na usnesení");
        var zapisovatel = zas.Zastupitele.First(z => z.JeZapisovatel);
        zapisovatel.JePritomen = false;
        var chybaRole = app.Core.Chyby.First(c => c.Kod == "pritomnost");
        app.Core.PrejitNaChybuCmd.Execute(chybaRole);
        var zakladni = (BasicInfoViewModel)okno.UcBasicInfo.DataContext;
        Program.Overit(okno.TcZasedani.SelectedIndex == 0 && zakladni.ChosenZastupitel == zapisovatel, "GUI navigace na nepřítomného zapisovatele");
        Program.Overit(okno.StavovyRadekValidace.IsVisible, "GUI společný stavový řádek je viditelný");
        Program.Overit(!Najit<System.Windows.Controls.TextBlock>(okno.StavovyRadekValidace).Any(t => t.Text?.Contains("Podrobnosti po najetí") == true),
          "GUI stavový řádek neobsahuje nadbytečný pomocný popisek");
        okno.StavovyRadekValidace.TlacitkoValidace.IsChecked = true;
        await Task.Delay(500);
        UlozitSnimek(okno, "validace.png");
        var seznamChyb = okno.StavovyRadekValidace.SeznamChyb;
        Program.Overit(okno.StavovyRadekValidace.TlacitkoValidace.ActualWidth > 150, "GUI text stavového řádku není oříznutý");
        Program.Overit(seznamChyb.IsOpen, "GUI kliknutí otevře seznam chyb");
        UlozitSnimek((FrameworkElement)seznamChyb.Child, "validace-seznam.png");
        var tlacitkoChyby = Najit<System.Windows.Controls.Button>(seznamChyb.Child).First(b => Equals(b.CommandParameter, chybaUsneseni));
        Program.Overit(tlacitkoChyby.Command != null, "GUI položka validace má navázaný příkaz");
        var automatizace = new System.Windows.Automation.Peers.ButtonAutomationPeer(tlacitkoChyby);
        ((System.Windows.Automation.Provider.IInvokeProvider)automatizace.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
        await Task.Delay(300);
        Program.Overit(okno.TcZasedani.SelectedIndex == 1 && program.ChosenProgramEntry == podbod && !seznamChyb.IsOpen, "GUI kliknutí na chybu přepne záložku a zavře seznam");
        var tooltip = (System.Windows.Controls.ToolTip)okno.StavovyRadekValidace.TlacitkoValidace.ToolTip;
        tooltip.PlacementTarget = okno.StavovyRadekValidace.TlacitkoValidace;
        tooltip.IsOpen = true;
        await Task.Delay(300);
        Program.Overit(Najit<System.Windows.Controls.ItemsControl>(tooltip).Single().Items.Count == app.Core.Chyby.Count, "GUI tooltip obsahuje všechny chyby");
        UlozitSnimek(tooltip, "validace-tooltip.png");
        tooltip.IsOpen = false;
        okno.TcZasedani.SelectedIndex = 1;
        Program.Overit(okno.StavovyRadekValidace.IsVisible, "GUI stavový řádek zůstává na programu");
        okno.TcZasedani.SelectedIndex = 2;
        Program.Overit(okno.StavovyRadekValidace.IsVisible, "GUI stavový řádek zůstává u generátoru");
        okno.TcZasedani.SelectedIndex = 0;
        zakladni.ZrusitRoleCmd.Execute(null);
        Program.Overit(!zapisovatel.JeZapisovatel && !app.Core.Chyby.Any(c => c.Kod == "pritomnost"), "GUI odebrání neplatné role");
        // Chybně napsané číslo nesmí ponechat starou hodnotu tiše exportovatelnou.
        var uroven = PresentationTraceSources.DataBindingSource.Switch.Level;
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
        okno.UcBasicInfo.TbPoradi.Text = "abc";
        await Task.Delay(100);
        Program.Overit(!app.Core.VstupyPlatne && app.Core.Chyby.Any(c => c.Kod == "vstup"), "GUI nečíselný vstup ve společné validaci");
        bool ulozeniBlokovano = false;
        try { app.Core.SaveZasedani(false); } catch (InvalidOperationException) { ulozeniBlokovano = true; }
        Program.Overit(ulozeniBlokovano, "GUI nečíselný vstup nelze tiše uložit");
        okno.UcBasicInfo.TbPoradi.Text = "1";
        await Task.Delay(100);
        Program.Overit(app.Core.VstupyPlatne, "GUI oprava číselného vstupu");
        PresentationTraceSources.DataBindingSource.Switch.Level = uroven;
        File.WriteAllText(Path.Combine(Program.Vystupy, "binding-errors.txt"), vazby.Text);
        Program.Overit(!vazby.Text.Contains("System.Windows.Data Error"), "GUI bez chyb datových vazeb");
        kod = 0;
        Console.WriteLine($"OK: {Program.Pocet} kontrol včetně GUI.");
      }
      catch (Exception ex) { Console.Error.WriteLine(ex); File.WriteAllText(Path.Combine(Program.Vystupy, "gui-error.txt"), ex + "\n" + vazby.Text); }
      finally { app.Core.Zasedani = null; app.Shutdown(); }
    }));
    Console.WriteLine("GUI: spouštím smyčku");
    app.Run();
    Console.WriteLine("GUI: smyčka skončila");
    return kod;
  }

  private static async Task OveritAkceProgramuAsync(MainWindow okno)
  {
    var usneseni = Najit<Resolution>(okno.UcProgram).Single();
    usneseni.TabulkaHlasovani.UpdateLayout();
    var obsahyHlasovani = usneseni.TabulkaHlasovani.Items.Cast<object>()
      .SelectMany(polozka => usneseni.TabulkaHlasovani.Columns.Select(sloupec => sloupec.GetCellContent(polozka)))
      .Where(obsah => obsah != null).Cast<FrameworkElement>().ToList();
    Program.Overit(obsahyHlasovani.Count > 0 && obsahyHlasovani.All(JeVertikalneUprostred),
      "GUI obsah tabulky hlasování je vertikálně uprostřed");
    var jmenaHlasujicich = Najit<System.Windows.Controls.StackPanel>(usneseni.TabulkaHlasovani)
      .Where(p => p.DataContext is HlasovaniZastupitele && p.Children.OfType<System.Windows.Controls.TextBlock>().Count() == 2).ToList();
    Program.Overit(jmenaHlasujicich.Count > 0 && jmenaHlasujicich.All(p => p.Orientation == System.Windows.Controls.Orientation.Horizontal) &&
      jmenaHlasujicich.Where(p => string.IsNullOrEmpty(((System.Windows.Controls.TextBlock)p.Children[1]).Text))
        .All(p => ((System.Windows.Controls.TextBlock)p.Children[1]).Visibility == Visibility.Collapsed),
      "GUI jména hlasujících nemají prázdný druhý řádek");
    var editorBodu = okno.UcProgram.UcProgramEntry;
    editorBodu.UpdateLayout();
    double spodekTlacitek = editorBodu.PanelAkciUsneseni.TranslatePoint(
      new Point(0, editorBodu.PanelAkciUsneseni.ActualHeight), editorBodu).Y;
    double vrsekSeznamu = editorBodu.SeznamUsneseni.TranslatePoint(new Point(0, 0), editorBodu).Y;
    Program.Overit(vrsekSeznamu - spodekTlacitek >= 7, "GUI seznam usnesení má mezeru pod tlačítky");
    var aplikace = (App)Application.Current;
    var zkusebniPrimarni = Color.FromRgb(36, 112, 168);
    aplikace.AdjustThemeContrastAndColors(zkusebniPrimarni, Color.FromRgb(170, 90, 40));
    await Task.Delay(100);
    var vybranyBod = (System.Windows.Controls.ListViewItem)okno.UcProgram.LvProgram.ItemContainerGenerator.ContainerFromItem(okno.UcProgram.LvProgram.SelectedItem);
    var vybraneUsneseni = (System.Windows.Controls.ListBoxItem)editorBodu.SeznamUsneseni.ItemContainerGenerator.ContainerFromItem(editorBodu.SeznamUsneseni.SelectedItem);
    var ocekavanePozadi = Color.FromArgb(0x38, zkusebniPrimarni.R, zkusebniPrimarni.G, zkusebniPrimarni.B);
    var ocekavanyOkraj = Color.FromArgb(0xCC, zkusebniPrimarni.R, zkusebniPrimarni.G, zkusebniPrimarni.B);
    Program.Overit(vybranyBod.Background is SolidColorBrush pozadiBodu && pozadiBodu.Color == ocekavanePozadi &&
      vybraneUsneseni.Background is SolidColorBrush pozadiUsneseni && pozadiUsneseni.Color == ocekavanePozadi &&
      vybranyBod.BorderBrush is SolidColorBrush okrajBodu && okrajBodu.Color == ocekavanyOkraj &&
      vybraneUsneseni.BorderBrush is SolidColorBrush okrajUsneseni && okrajUsneseni.Color == ocekavanyOkraj,
      "GUI výběr bodu i usnesení dynamicky přebírá průhlednou primární barvu");
    aplikace.AdjustThemeContrastAndColors((Color)aplikace.FindResource("ThemeColorMain"), (Color)aplikace.FindResource("ThemeColorAccent"));
    await Task.Delay(100);
    var model = (ResolutionViewModel)usneseni.DataContext;
    var puvodniHlasy = model.Usneseni.VolbyZastupitelu.Select(h => h.Volba).ToList();
    model.AllAgreeCmd.Execute(null);
    await Task.Delay(100);
    Program.Overit(usneseni.IkonaHlasovani.Kind == MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline, "GUI přijaté usnesení má zelenou fajfku");
    model.AllDisagreeCmd.Execute(null);
    await Task.Delay(100);
    Program.Overit(usneseni.IkonaHlasovani.Kind == MaterialDesignThemes.Wpf.PackIconKind.AlertCircle, "GUI nepřijaté usnesení má výstražnou ikonu");
    var tooltip = (System.Windows.Controls.ToolTip)usneseni.StavHlasovani.ToolTip;
    tooltip.PlacementTarget = usneseni.StavHlasovani;
    tooltip.IsOpen = true;
    await Task.Delay(150);
    tooltip.UpdateLayout();
    UlozitSnimek(tooltip, "hlasovani-tooltip.png");
    var texty = Najit<System.Windows.Controls.TextBlock>(tooltip).Select(t => t.Text).ToList();
    Program.Overit(texty.Contains(model.Vyhodnoceni.Vysledek) && texty.Contains(model.Vyhodnoceni.Souhrn) &&
      texty.Any(t => t.Contains("Potřebný počet hlasů pro:")), "GUI tooltip hlasování obsahuje výsledek, počty i potřebnou většinu");
    tooltip.IsOpen = false;
    for (int i = 0; i < puvodniHlasy.Count; i++) model.Usneseni.VolbyZastupitelu[i].Volba = puvodniHlasy[i];
    var program = okno.UcProgram;
    program.AkceBodu.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
    await Task.Delay(150);
    var nabidka = program.AkceBodu.ContextMenu;
    var presuny = (System.Windows.Controls.MenuItem)nabidka.Items[0];
    Program.Overit(Equals(presuny.Header, "Upravit"), "GUI podnabídka přesunů má stručný název Upravit");
    presuny.IsSubmenuOpen = true;
    await Task.Delay(150);
    Program.Overit(nabidka.IsOpen && presuny.Items.OfType<System.Windows.Controls.MenuItem>().Count() == 4 &&
      presuny.Items.OfType<System.Windows.Controls.MenuItem>().All(p => p.Command != null),
      "GUI šipkové akce jsou v podmenu s funkčními příkazy");
    presuny.IsSubmenuOpen = false;
    nabidka.IsOpen = false;
    program.PridatBod.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
    await Task.Delay(100);
    Program.Overit(program.PridatBod.ContextMenu.Items.OfType<System.Windows.Controls.MenuItem>().Any(p =>
      Equals(p.Header, "Podbod vybraného bodu") && p.Command != null), "GUI přidání podbodu zůstává dostupné v nabídce");
    program.PridatBod.ContextMenu.IsOpen = false;
    var sloupec = Najit<System.Windows.Controls.Grid>(program).First(g => g.ColumnDefinitions.Count == 3).ColumnDefinitions[0];
    var sirka = sloupec.Width;
    sloupec.Width = new GridLength(230);
    program.UpdateLayout();
    Program.Overit(program.PanelAkci.ActualHeight <= 40 && program.PridatBod.ActualWidth > 75 &&
      program.AkceBodu.ActualWidth > 60, "GUI akce zůstávají na jediném řádku i v nejužším panelu");
    sloupec.Width = sirka;
    program.UpdateLayout();
  }

  private static async Task OveritSeznamZastupiteluAsync(MainWindow okno)
  {
    Program.Overit(okno.UcBasicInfo.PlaceholderBezZnaku.Visibility == Visibility.Visible,
      "GUI bez loga zobrazí placeholder Bez znaku");
    var zkusebniBitmapa = new WriteableBitmap(2, 2, 96, 96, PixelFormats.Bgra32, null);
    zkusebniBitmapa.WritePixels(new Int32Rect(0, 0, 2, 2),
      new byte[] { 80, 170, 40, 255, 80, 170, 40, 255, 40, 90, 180, 255, 40, 90, 180, 255 }, 8, 0);
    var kodovaniPng = new PngBitmapEncoder();
    kodovaniPng.Frames.Add(BitmapFrame.Create(zkusebniBitmapa));
    using (var proudPng = new MemoryStream())
    {
      kodovaniPng.Save(proudPng);
      ((BasicInfoViewModel)okno.UcBasicInfo.DataContext).Core.Zasedani.LogoObceData = proudPng.ToArray();
    }
    await Task.Delay(100);
    Program.Overit(okno.UcBasicInfo.PlaceholderBezZnaku.Visibility == Visibility.Collapsed,
      "GUI placeholder se po výběru znaku skryje");
    ((BasicInfoViewModel)okno.UcBasicInfo.DataContext).Core.Zasedani.LogoObce = null;
    await Task.Delay(100);
    var seznam = okno.UcBasicInfo.LvZastupitele;
    seznam.UpdateLayout();
    var zaskrtavaciPole = Najit<System.Windows.Controls.CheckBox>(seznam).ToList();
    Program.Overit(zaskrtavaciPole.Count >= 5 && zaskrtavaciPole.All(p => ReferenceEquals(p.Template, zaskrtavaciPole[0].Template)), "GUI všechny checkboxové sloupce mají jednotnou šablonu");
    Program.Overit(zaskrtavaciPole.All(JeVertikalneUprostred), "GUI všechny checkboxy jsou vertikálně uprostřed buněk");
    var textovaZahlavi = Najit<System.Windows.Controls.Primitives.DataGridColumnHeader>(seznam)
      .Where(h => Equals(h.Content, "Jméno") || Equals(h.Content, "Příjmení")).ToList();
    Program.Overit(textovaZahlavi.Count == 2 && textovaZahlavi.All(h => h.HorizontalContentAlignment == HorizontalAlignment.Left),
      "GUI záhlaví jména a příjmení jsou zarovnána doleva");
    for (int sloupec = 1; sloupec <= 2; sloupec++)
    {
      seznam.CurrentCell = new System.Windows.Controls.DataGridCellInfo(seznam.Items[0], seznam.Columns[sloupec]);
      seznam.Focus();
      Program.Overit(seznam.BeginEdit(), "GUI lze otevřít editor " + seznam.Columns[sloupec].Header);
      seznam.UpdateLayout();
      await Task.Delay(100);
      var editor = seznam.Columns[sloupec].GetCellContent(seznam.Items[0]) as System.Windows.Controls.TextBox;
      Program.Overit(editor != null && JeVertikalneUprostred(editor) && editor.VerticalContentAlignment == VerticalAlignment.Center &&
        editor.TextAlignment == TextAlignment.Left && editor.Margin == new Thickness(0),
        "GUI editor " + seznam.Columns[sloupec].Header + " je svisle uprostřed a vodorovně vlevo");
      UlozitSnimek(seznam, "zastupitele-editor-" + sloupec + ".png");
      seznam.CancelEdit();
    }
    seznam.UpdateLayout();
  }

  private static bool JeVertikalneUprostred(FrameworkElement prvek)
  {
    DependencyObject rodic = VisualTreeHelper.GetParent(prvek);
    while (rodic != null && rodic is not System.Windows.Controls.DataGridCell) rodic = VisualTreeHelper.GetParent(rodic);
    if (rodic is not System.Windows.Controls.DataGridCell bunka) return false;
    var pozice = prvek.TransformToAncestor(bunka).Transform(new Point(0, 0));
    return Math.Abs(pozice.Y + prvek.ActualHeight / 2 - bunka.ActualHeight / 2) <= 1;
  }

  private static async Task PockatAsync(Func<Task<bool>> podminka)
  {
    var limit = DateTime.UtcNow.AddSeconds(30);
    while (DateTime.UtcNow < limit) { if (await podminka()) return; await Task.Delay(100); }
    throw new TimeoutException("GUI nebylo včas připraveno.");
  }
  private static IEnumerable<T> Najit<T>(DependencyObject rodic) where T : DependencyObject
  {
    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(rodic); i++)
    {
      var dite = VisualTreeHelper.GetChild(rodic, i);
      if (dite is T t) yield return t;
      foreach (var dalsi in Najit<T>(dite)) yield return dalsi;
    }
  }
  private static void UlozitSnimek(FrameworkElement okno, string nazev)
  {
    var bitmapa = new RenderTargetBitmap((int)okno.ActualWidth, (int)okno.ActualHeight, 96, 96, PixelFormats.Pbgra32);
    bitmapa.Render(okno);
    var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmapa));
    using var proud = File.Create(Path.Combine(Program.Vystupy, nazev)); png.Save(proud);
  }
  private sealed class ZachytitVazby : TraceListener
  {
    private readonly System.Text.StringBuilder _text = new();
    public string Text => _text.ToString();
    public override void Write(string message) => _text.Append(message);
    public override void WriteLine(string message) => _text.AppendLine(message);
  }
}
