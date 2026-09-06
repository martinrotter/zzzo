using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ZZZO.Common.API;
using ZZZO.Common.Validace;

namespace ZZZO.Common.Generators
{
  internal static class Extensions
  {
    #region Metody

    public static XmlElement AppendClass(this XmlElement element, string clas)
    {
      string currentClas = element.GetAttribute("class") ?? string.Empty;
      element.SetAttribute("class", string.IsNullOrWhiteSpace(currentClas) ? clas : $"{currentClas} {clas}");
      return element;
    }

    public static XmlElement AppendElem(this XmlElement parent, string name)
    {
      XmlElement elem = parent.OwnerDocument.CreateElement(name);
      parent.AppendChild(elem);
      return elem;
    }

    public static XmlElement SetAttr(this XmlElement element, string attrName, string attrValue)
    {
      element.SetAttribute(attrName, attrValue);
      return element;
    }

    #endregion
  }

  public class GeneratorHtmlParams
  {
    #region Vlastnosti

    public string HtmlStyle
    {
      get;
      set;
    }

    public Generator.TypDokumentu KindOfDocument
    {
      get;
      set;
    }

    #endregion
  }

  public class GeneratorHtml : Generator
  {
    #region Vlastnosti

    public override string FileSuffix
    {
      get => "html";
    }

    public List<TypDokumentu> KindsOfDocuments
    {
      get;
    } = new List<TypDokumentu>
    {
      TypDokumentu.Pozvanka,
      TypDokumentu.Zapis
    };

    public List<string> Styles
    {
      get;
    } = Directory.GetFiles(Constants.PathsAndFiles.AppStylesFolder, "*.css", SearchOption.TopDirectoryOnly)
      .Select(Path.GetFileName).ToList();

    public override string Title
    {
      get => "HTML";
    }

    #endregion

    #region Metody

    public static string GetStyle(string styleName)
    {
      return File.ReadAllText(Path.Combine(Constants.PathsAndFiles.AppStylesFolder, styleName));
    }

    protected override byte[] GenerateDoWork(Zasedani zas, IProgress<int> progress, object param)
    {
      GeneratorHtmlParams prms = (GeneratorHtmlParams)param;

      var chyby = ValidatorZasedani.Overit(zas).Where(c => c.Blokuje(prms.KindOfDocument)).ToList();
      if (chyby.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, chyby.Select(c => c.Zprava)));
      progress?.Report(1);

      XmlDocument html = new XmlDocument();
      XmlElement htmlElem = html.CreateElement("html");

      html.AppendChild(htmlElem);

      GenerateHeader(htmlElem, zas, progress, prms);

      switch (prms.KindOfDocument)
      {
        case TypDokumentu.Zapis:
          GenerateRecordBody(htmlElem, zas, progress);
          break;

        case TypDokumentu.Pozvanka:
        default:
          GenerateInvitationBody(htmlElem, zas, progress);
          break;
      }

      progress?.Report(100);

      return DumpXmlToHtml(html);
    }

    private byte[] DumpXmlToHtml(XmlDocument html)
    {
      // XML by jinak prázdné HTML bloky zapsalo jako <div/>, což HTML parser chápe jinak.
      var prazdne = html.SelectNodes("//*[not(node())]").Cast<XmlElement>().ToList();
      foreach (var prvek in prazdne)
        if (!new[] { "img", "br", "hr", "meta", "link", "input", "wbr", "col", "source" }.Contains(prvek.Name))
          prvek.AppendChild(html.CreateTextNode(""));
      string vystup = html.OuterXml;
      foreach (XmlElement styl in html.GetElementsByTagName("style"))
        vystup = vystup.Replace(styl.OuterXml, "<style>" + styl.InnerText + "</style>");
      return Encoding.UTF8.GetBytes("<!DOCTYPE html>\n" + vystup);
    }

    private void GenerateInvitationBody(XmlElement html, Zasedani zas, IProgress<int> progress)
    {
      XmlElement body = html.AppendElem("body");

      if (zas.LogoObce != null)
      {
        body.AppendElem("img").AppendClass("logo").SetAttr("src", $"data:image/png;base64,{Convert.ToBase64String(zas.LogoObceData)}");
      }

      body.AppendElem("h1").AppendClass("text-center").InnerText = $"Pozvánka na {zas.Poradi}. zasedání zastupitelstva obce {zas.NazevObce}";
      body.AppendElem("p").InnerText =
        $"Starosta obce {zas.NazevObce} podle \u00a7 103 odst. 5 zákona č. 128/2000 Sb. o obcích svolává " +
        $"{zas.Poradi}. zasedání zastupitelstva obce {zas.NazevObce}.";

      var dateTimeBox = body.AppendElem("div").AppendClass("resolution-vote-box").AppendClass("success");

      dateTimeBox.AppendElem("p").InnerText = $"Datum konání: {zas.DatumCasKonani:dddd, d. M. yyyy v H:mm} SEČ";
      dateTimeBox.AppendElem("p").InnerText = $"Místo konání: {zas.AdresaKonani}, v {zas.AdresaKonani.PopisMista}";

      body.AppendElem("h2").InnerText = "Navržený program";

      var bodyProgramu = zas.Program.BodyProgramu.Where(b => b.JeBezny).ToList();
      GenerateProgramEntries(body, bodyProgramu);
    }

    private void GenerateRecordBody(XmlElement html, Zasedani zas, IProgress<int> progress)
    {
      int lastResolutionNumber = 0;
      var cislaBodu = zas.Program.OcislovatBody();
      List<string> acceptedResolutions = new List<string>();
      Zastupitel ridici = zas.Zastupitele.FirstOrDefault(zs => zs.JeRidici);

      if (ridici == null)
      {
        throw new Exception("není vybráná řídící osoba pro toto zasedání");
      }

      Zastupitel starosta = zas.Zastupitele.FirstOrDefault(zs => zs.JeStarosta);

      if (starosta == null)
      {
        throw new Exception("není vybrán starosta obce");
      }

      Zastupitel zapisovatel = zas.Zastupitele.FirstOrDefault(zs => zs.JeZapisovatel);

      if (zapisovatel == null)
      {
        throw new Exception("není vybrán zapisovatel");
      }

      IEnumerable<Zastupitel> overovatele = zas.Zastupitele.Where(zs => zs.JeOverovatel);

      if (!overovatele.Any())
      {
        throw new Exception("nejsou vybráni ověřovatelé");
      }

      XmlElement body = html.AppendElem("body");

      if (zas.LogoObce != null)
      {
        body.AppendElem("img").AppendClass("logo").SetAttr("src", $"data:image/png;base64,{Convert.ToBase64String(zas.LogoObceData)}");
      }

      body.AppendElem("h1").AppendClass("text-center").InnerText = $"Zápis z {zas.Poradi}. zasedání zastupitelstva obce " +
                                                                   $"{zas.NazevObce} konaného dne {zas.DatumCasKonani:d. M. yyyy}";

      ///
      /// Zahájení.
      ///
      body.AppendElem("h2").InnerText = "Zahájení";

      body.AppendElem("p").InnerText =
        $"Zasedání zastupitelstva obce (dále jen ZO) {zas.NazevObce} bylo zahájeno dne " +
        $"{zas.DatumCasKonani:d. M. yyyy v H:mm} SEČ na adrese {zas.AdresaKonani} v {zas.AdresaKonani.PopisMista}.";

      body.AppendElem("p").InnerText =
        $"Zúčastnění zastupitelé: {string.Join(
          ", ",
          zas.Zastupitele
            .Where(zs => zs.JePritomen)
            .OrderBy(zs => zs.Prijmeni)
            .Select(zs => zs.Jmeno + " " + zs.Prijmeni))}.";

      string nepritJmena = string.Join(
        ", ",
        zas.Zastupitele
          .Where(zs => !zs.JePritomen)
          .OrderBy(zs => zs.Prijmeni)
          .Select(zs => zs.Jmeno + " " + zs.Prijmeni));

      body.AppendElem("p").InnerText =
        $"Nepřítomní zastupitelé: {(string.IsNullOrWhiteSpace(nepritJmena) ? "-" : nepritJmena + ".")}";

      body.AppendElem("p").InnerText =
        $"Zasedání ZO {Sklonovat("navštívil", "navštívili", "navštívilo", zas.PocetHostu)} {zas.PocetHostu} " +
        $"{Sklonovat("host", "hosté", "hostů", zas.PocetHostu)} z řad veřejnosti.";

      body.AppendElem("p").InnerText =
        $"Zasedání ZO řídil pan {ridici.Jmeno} {ridici.Prijmeni}.";

      body.AppendElem("p").InnerText = $"Všechna hlasování na tomto zasedání ZO {zas.NazevObce} " +
                                       "jsou veřejná a zastupitelé hlasují zdvižením ruky.";

      progress?.Report(30);

      foreach (var bod in zas.Program.BodyProgramu)
      {
        VytvoritBod(bod, false);
        foreach (var podbod in bod.Podbody) VytvoritBod(podbod, true);
      }

      void VytvoritBod(BodProgramu bod, bool jePodbod)
      {
        body.AppendElem(jePodbod ? "h3" : "h2").SetAttr("id", "bod-" + bod.Id.ToString("N"))
          .InnerText = cislaBodu.GetValueOrDefault(bod.Id, "") + bod.Nadpis +
          (bod.Typ == BodProgramu.TypBoduProgramu.DoplnenyBodZasedani ? " (doplněný bod programu)" : "");
        if (bod.Typ == BodProgramu.TypBoduProgramu.SchvaleniZapisOver)
          body.AppendElem("p").InnerText = $"Řídící osoba zasedání ZO {zas.NazevObce} navrhla, aby zapisovatelem byl {zapisovatel.Jmeno} {zapisovatel.Prijmeni} a ověřovateli zápisu byli {string.Join(" a ", overovatele.Select(z => z.Jmeno + " " + z.Prijmeni))}.";
        if (bod.Typ == BodProgramu.TypBoduProgramu.SchvaleniProgramu)
        {
          GenerateProgramEntries(body, zas.Program.BodyProgramu.Where(b => b.JeBezny));
          body.AppendElem("p").InnerText = $"Řídící osoba zasedání ZO {zas.NazevObce} navrhla schválit výše uvedený návrh programu.";
        }
        if (!string.IsNullOrWhiteSpace(bod.PrubehHtml)) HtmlObsah.Vlozit(body.AppendElem("div"), bod.PrubehHtml);
        if (bod.Rezim == RezimBodu.BereNaVedomi)
          body.AppendElem("div").AppendClass("resolution-container").AppendElem("p").AppendClass("resolution-text")
            .InnerText = $"ZO {zas.NazevObce} bere na vědomí.";
        if (bod.Rezim != RezimBodu.SUsnesenimi) return;
        foreach (var u in bod.Usneseni)
        {
          string hlasovani = bod.Typ switch
          {
            BodProgramu.TypBoduProgramu.SchvaleniZapisOver => "Hlasování o navrženém zapisovateli a ověřovatelích zápisu",
            BodProgramu.TypBoduProgramu.SchvaleniProgramu => "Hlasování o návrhu programu",
            _ => null
          };
          var text = GenerateResolution(body, zas, bod, u, lastResolutionNumber, hlasovani);
          if (text != null) acceptedResolutions.Add(text);
          // Procedurální hlasování si zachovávají dosavadní samostatný režim číslování.
          if (bod.JeBezny) lastResolutionNumber++;
        }
      }

      body.AppendElem("h2").AppendClass("break-it").InnerText = "Přijatá usnesení";

      foreach (string acceptedResolution in acceptedResolutions)
      {
        HtmlObsah.Vlozit(body.AppendElem("div"), acceptedResolution);
      }

      body.AppendElem("hr").AppendClass("resolution-list-line");
      body.AppendElem("p").InnerText = $"Celkový počet přijatých usnesení je {acceptedResolutions.Count}.";

      progress?.Report(80);

      ///
      /// Podpisy.
      ///
      XmlElement sigWrapper = body.AppendElem("div").AppendClass("signature-wrapper");

      sigWrapper.AppendElem("div").AppendClass("signature").InnerXml =
        "<hr/>" +
        $"{HtmlObsah.Zakodovat(starosta.Jmeno)} {HtmlObsah.Zakodovat(starosta.Prijmeni)}<br/>" +
        "starosta obce";

      foreach (Zastupitel overovatel in overovatele)
      {
        sigWrapper.AppendElem("div").AppendClass("signature").InnerXml =
          "<hr/>" +
          $"{HtmlObsah.Zakodovat(overovatel.Jmeno)} {HtmlObsah.Zakodovat(overovatel.Prijmeni)}<br/>" +
          "ověřovatel zápisu";
      }
    }

    private void GenerateHeader(
      XmlElement html,
      Zasedani zas,
      IProgress<int> progress,
      GeneratorHtmlParams prms)
    {
      XmlElement head = html.AppendElem("head");

      head.AppendElem("style").InnerText = GetStyle(prms.HtmlStyle ?? Styles.First());
      head.AppendElem("meta").SetAttr("charset", "UTF-8");
      head.AppendElem("meta").SetAttr("name", "viewport").SetAttr("content", "width=device-width, initial-scale=1.0");

      switch (prms.KindOfDocument)
      {
        case TypDokumentu.Pozvanka:
          head.AppendElem("title").InnerText = $"Pozvánka na {zas.Poradi}. zasedání zastupitelstva obce " +
                                               $"{zas.NazevObce}, které se bude konat dne {zas.DatumCasKonani:d. M. yyyy}";
          break;

        case TypDokumentu.Zapis:
          head.AppendElem("title").InnerText = $"Zápis z {zas.Poradi}. zasedání zastupitelstva obce " +
                                               $"{zas.NazevObce} konaného dne {zas.DatumCasKonani:d. M. yyyy}";
          break;
      }



      progress?.Report(10);
    }

    private void GenerateProgramEntries(XmlElement body, IEnumerable<BodProgramu> bodyProgramu)
    {
      var seznam = body.AppendElem("div").AppendClass("program").AppendElem("ol").AppendClass("ol-verbatim");
      int poradi = 0;
      foreach (var bod in bodyProgramu)
      {
        if (!bod.JeBezny) continue;
        var radek = seznam.AppendElem("li");
        radek.InnerText = $"{++poradi}. {bod.Nadpis}" + (bod.Typ == BodProgramu.TypBoduProgramu.DoplnenyBodZasedani ? " (doplněný bod programu)" : "");
        if (bod.Podbody.Count == 0) continue;
        var podseznam = radek.AppendElem("ol").AppendClass("ol-verbatim");
        var cisla = new Program();
        cisla.BodyProgramu.Add(bod);
        foreach (var podbod in bod.Podbody)
        {
          var oznaceni = cisla.OcislovatBody()[podbod.Id];
          oznaceni = poradi + oznaceni.Substring(1);
          podseznam.AppendElem("li").InnerText = oznaceni + podbod.Nadpis +
            (podbod.Typ == BodProgramu.TypBoduProgramu.DoplnenyBodZasedani ? " (doplněný bod programu)" : "");
        }
      }
    }

    private string GenerateResolution(
      XmlElement body, Zasedani zas, BodProgramu programEntry, Usneseni resolution, int lastOrder, string replacementTitle = null)
    {
      string generatedResolutionTitle = null;
      XmlElement root = body.AppendElem("div").AppendClass("resolution-container").SetAttr("id", "usneseni-" + resolution.Id.ToString("N"));
      if (programEntry.JeBezny)
      {
        root.AppendElem("p").AppendClass("resolution-text-heading").InnerText = "Návrh usnesení:";
        var obsah = root.AppendElem("div").AppendClass("resolution-text");
        VlozitTextUsneseni(obsah, $"Č. {lastOrder + 1}-{zas.DatumCasKonani.Month}/{zas.DatumCasKonani.Year} - ", resolution.TextHtml);
        generatedResolutionTitle = obsah.InnerXml;
      }
      else
        HtmlObsah.Vlozit(root.AppendElem("div").AppendClass("resolution-text"), resolution.TextHtml);

      int countOfPresentVoters = zas.Zastupitele.Count(vol => vol.JePritomen);

      List<HlasovaniZastupitele> choiceFor = resolution.VolbyZastupitelu.Where(vol =>
        vol.Zastupitel.JePritomen &&
        vol.Volba == HlasovaniZastupitele.VolbaHlasovani.Pro).ToList();

      List<HlasovaniZastupitele> choiceAgainst = resolution.VolbyZastupitelu.Where(vol =>
        vol.Zastupitel.JePritomen &&
        vol.Volba == HlasovaniZastupitele.VolbaHlasovani.Proti).ToList();

      List<HlasovaniZastupitele> choiceDontKnow = resolution.VolbyZastupitelu.Where(vol =>
        vol.Zastupitel.JePritomen &&
        vol.Volba == HlasovaniZastupitele.VolbaHlasovani.ZdrzujeSe).ToList();

      string choiceForStr = choiceFor.Count() + (choiceFor.Any() && choiceFor.Count < countOfPresentVoters
        ? $" ({string.Join(", ", choiceFor.Select(ch => HtmlObsah.Zakodovat(ch.Zastupitel.Jmeno + " " + ch.Zastupitel.Prijmeni)))})"
        : string.Empty);

      string choiceAgainstStr = choiceAgainst.Count() + (choiceAgainst.Any() && choiceAgainst.Count < countOfPresentVoters
        ? $" ({string.Join(", ", choiceAgainst.Select(ch => HtmlObsah.Zakodovat(ch.Zastupitel.Jmeno + " " + ch.Zastupitel.Prijmeni)))})"
        : string.Empty);

      string choiceDontKnowStr = choiceDontKnow.Count() + (choiceDontKnow.Any() && choiceDontKnow.Count < countOfPresentVoters
        ? $" ({string.Join(", ", choiceDontKnow.Select(ch => HtmlObsah.Zakodovat(ch.Zastupitel.Jmeno + " " + ch.Zastupitel.Prijmeni)))})"
        : string.Empty);

      bool accepted = VyhodnoceniHlasovani.Vyhodnotit(zas, resolution).JeSchvaleno;

      XmlElement div = root.AppendElem("div").AppendClass("resolution-vote-box").AppendClass(accepted ? "success" : "failure");

      div.AppendElem("p").AppendClass("resolution-vote-heading").InnerText = $"{replacementTitle ?? "Hlasování o návrhu usnesení"}:";

      div.AppendElem("p").InnerXml =
        $"<span class=\"resolution-vote resolution-success-icon\">\u2713</span> PRO: {choiceForStr}<br/>" +
        $"<span class=\"resolution-vote resolution-failure-icon\">\u00D7</span> PROTI: {choiceAgainstStr}<br/>" +
        $"<span class=\"resolution-vote resolution-dontknow-icon\">?</span> ZDRŽUJE SE: {choiceDontKnowStr}";

      if (accepted)
      {
        div.AppendElem("p")
          .AppendClass("resolution-decision-box")
          .AppendClass("resolution-decision-success")
          .AppendClass("resolution-success").InnerXml = "<span class=\"resolution-success-icon\">\u2713</span> Návrh byl přijat.";
      }
      else
      {
        div.AppendElem("p")
          .AppendClass("resolution-decision-box")
          .AppendClass("resolution-decision-failure")
          .AppendClass("resolution-failure").InnerXml = "<span class=\"resolution-failure-icon\">\u00D7</span> Návrh nebyl přijat.";
      }

      return accepted ? generatedResolutionTitle : null;
    }

    private static void VlozitTextUsneseni(XmlElement cil, string prefix, string html)
    {
      HtmlObsah.Vlozit(cil, html);
      var prvni = cil.FirstChild as XmlElement;
      if (prvni?.Name == "p") prvni.PrependChild(cil.OwnerDocument.CreateTextNode(prefix));
      else
      {
        var odstavec = cil.OwnerDocument.CreateElement("p");
        odstavec.InnerText = prefix;
        cil.PrependChild(odstavec);
      }
    }

    private string Sklonovat(string jednaPolozka, string dvePolozky, string vicePolozek, int pocet)
    {
      switch (pocet)
      {
        case 1:
          return jednaPolozka;

        case 2:
        case 3:
        case 4:
          return dvePolozky;

        default:
          return vicePolozek;
      }
    }

    #endregion
  }
}
