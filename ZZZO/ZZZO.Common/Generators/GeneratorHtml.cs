using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ZZZO.Common.API;

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

  public class GeneratorHtml : Generator
  {
    #region Vlastnosti

    public override string FileSuffix
    {
      get => "html";
    }

    public override string Title
    {
      get => "HTML";
    }

    #endregion

    #region Metody

    protected override byte[] GenerateDoWork(Zasedani zas, IProgress<int> progress)
    {
      progress.Report(1);

      XmlDocument html = new XmlDocument();
      XmlElement htmlElem = html.CreateElement("html");

      html.AppendChild(htmlElem);
      GenerateHeader(htmlElem, zas, progress);
      GenerateBody(htmlElem, zas, progress);

      progress.Report(100);

      return DumpXmlToHtml(html);
    }

    private string ConvertPlainTextToHtml(string text)
    {
      return Regex.Replace(text, "\\r\\n?", "<br/>");
    }

    private byte[] DumpXmlToHtml(XmlDocument html)
    {
      // NOTE: Replace encoded chars. Yes, this is hell.
      return Encoding.UTF8.GetBytes("<!DOCTYPE html>\n" + html.OuterXml
        .Replace("&gt;", ">")
        .Replace("&lt;", "<"));
    }

    private void GenerateBody(XmlElement html, Zasedani zas, IProgress<int> progress)
    {
      int lastResolutionNumber = 0;
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

      BodProgramu schvaleniProgramu = zas.Program.BodyProgramu.FirstOrDefault(prog => prog.SchvalovaniProgramu);

      if (schvaleniProgramu?.Usneseni == null || schvaleniProgramu.Usneseni.Count == 0)
      {
        throw new Exception("v programu chybí bod a usnesení pro schválení programu jako takového");
      }

      IEnumerable<BodProgramu> bodyProgramu = zas.Program.BodyProgramu.Where(prog => !prog.SchvalovaniProgramu);

      XmlElement body = html.AppendElem("body");

      if (zas.LogoObce != null)
      {
        body.AppendElem("img").AppendClass("logo").SetAttr("src", $"data:image/png;base64,{Convert.ToBase64String(zas.LogoObceData)}");
      }

      body.AppendElem("h1").AppendClass("text-center").InnerText = $"Zápis z {zas.Poradi}. zasedání zastupitelstva obce " +
                                                                   $"{zas.NazevObce} konaného dne {zas.DatumCas:d. M. yyyy}";

      ///
      /// Zahájení.
      /// 
      body.AppendElem("h2").InnerText = "Zahájení";

      body.AppendElem("p").InnerText =
        $"Zasedání zastupitelstva obce (dále jen ZO) {zas.NazevObce} bylo zahájeno dne " +
        $"{zas.DatumCas:d. M. yyyy v H:mm} SEČ na adrese {zas.AdresaKonani} v {zas.AdresaKonani.PopisMista}.";

      body.AppendElem("p").InnerText =
        $"Zúčastnění zastupitelé: {string.Join(
          ", ",
          zas.Zastupitele
            .Where(zs => zs.JePritomen)
            .OrderBy(zs => zs.Prijmeni)
            .Select(zs => zs.Jmeno + " " + zs.Prijmeni))}.";

      body.AppendElem("p").InnerText =
        $"Nepřítomní zastupitelé: {string.Join(
          ", ",
          zas.Zastupitele
            .Where(zs => !zs.JePritomen)
            .OrderBy(zs => zs.Prijmeni)
            .Select(zs => zs.Jmeno + " " + zs.Prijmeni))}.";

      body.AppendElem("p").InnerText = $"Zasedání ZO navštívilo {zas.PocetHostu} " +
                                       $"{Sklonovat("host", "hosté", "hostů", zas.PocetHostu)} z řad veřejnosti.";

      body.AppendElem("p").InnerText =
        $"Zasedání ZO řídil pan {ridici.Jmeno} {ridici.Prijmeni}.";

      progress.Report(30);

      ///
      /// Určení ověřovatelů atd.
      /// 
      body.AppendElem("h2").InnerText = "Určení ověřovatelů zápisu a zapisovatele v souladu s \u00a7 95 odst. 1 č. 128/2000 Sb.";

      body.AppendElem("p").InnerText =
        $"Řídící osoba zasedání ZO {zas.NazevObce} rozhodla, že zapisovatelem je " +
        $"{zapisovatel.Jmeno} {zapisovatel.Prijmeni} a ověřovateli zápisu jsou {string.Join(
          " a ",
          overovatele.Select(over => over.Jmeno + " " + over.Prijmeni))}.";

      progress.Report(40);

      ///
      /// Program a jeho schvalování.
      /// 
      body.AppendElem("h2").InnerText = "Schválení programu";

      GenerateProgramEntries(body, bodyProgramu.ToList());

      body.AppendElem("p").InnerText = "Pan řídící navrhl schválit výše uvedený návrh programu. " +
                                       $"Všechna hlasování na tomto zasedání ZO {zas.NazevObce} " +
                                       "jsou veřejná a zastupitelé hlasují zdvižením ruky.";

      GenerateResolution(body, zas, schvaleniProgramu, schvaleniProgramu.Usneseni.First(), lastResolutionNumber, "Hlasování o návrhu programu");

      progress.Report(50);

      ///
      /// Kontrola zápisu.
      /// 
      body.AppendElem("h2").InnerText = "Kontrola zápisu a plnění usnesení z minulého zasedání ZO";

      body.AppendElem("p").InnerText = "Starosta obce zhodnotil program z minulého jednání ZO a " +
                                       "informoval přítomné zastupitele i veřejnost o " +
                                       "projednaných bodech a splněných úkolech.";

      foreach (BodProgramu bodProgramu in bodyProgramu.Where(prog => !prog.SchvalovaniProgramu))
      {
        body.AppendElem(bodProgramu.JePodbod ? "h3" : "h2").InnerText = $"{bodProgramu.NadpisPoradi}{bodProgramu.Nadpis}{(bodProgramu.JeDoplneny ? " (doplněný bod programu)" : string.Empty)}";

        if (!string.IsNullOrWhiteSpace(bodProgramu.Text))
        {
          body.AppendElem("div").InnerXml = bodProgramu.Text;
        }

        foreach (Usneseni usneseni in bodProgramu.Usneseni)
        {
          if (GenerateResolution(body, zas, bodProgramu, usneseni, lastResolutionNumber) is string resolutionText)
          {
            acceptedResolutions.Add(resolutionText);
          }

          if (!usneseni.ZoBereNaVedomi)
          {
            lastResolutionNumber++;
          }
        }
      }

      body.AppendElem("h2").InnerText = "Přijatá usnesení";

      foreach (string acceptedResolution in acceptedResolutions)
      {
        body.AppendElem("p").InnerText = acceptedResolution;
      }

      body.AppendElem("hr");
      body.AppendElem("p").InnerText = $"Celkový počet přijatých usnesení je {acceptedResolutions.Count}.";

      progress.Report(80);

      ///
      /// Podpisy.
      ///
      XmlElement sigWrapper = body.AppendElem("div").AppendClass("signature-wrapper");

      sigWrapper.AppendElem("div").AppendClass("signature").AppendElem("p").InnerText =
        "<hr/><br/>" +
        $"{starosta.Jmeno} {starosta.Prijmeni}<br/>" +
        "starosta obce";

      foreach (Zastupitel overovatel in overovatele)
      {
        sigWrapper.AppendElem("div").AppendClass("signature").AppendElem("p").InnerText =
          "<hr/><br/>" +
          $"{overovatel.Jmeno} {overovatel.Prijmeni}<br/>" +
          "ověřovatel zápisu";
      }
    }

    private void GenerateHeader(XmlElement html, Zasedani zas, IProgress<int> progress)
    {
      XmlElement head = html.AppendElem("head");

      head.AppendElem("style").InnerText = GetStyle();
      head.AppendElem("meta").SetAttr("charset", "UTF-8");
      head.AppendElem("meta").SetAttr("name", "viewport").SetAttr("content", "width=device-width, initial-scale=1.0");

      head.AppendElem("title").InnerText = $"Zápis z {zas.Poradi}. zasedání zastupitelstva obce " +
                                           $"{zas.NazevObce} konaného dne {zas.DatumCas:d. M. yyyy}";

      progress.Report(10);
    }

    private void GenerateProgramEntries(XmlElement body, IEnumerable<BodProgramu> bodyProgramu)
    {
      XmlElement div = body.AppendElem("div");
      XmlElement mainOl = div.AppendElem("ol");
      XmlElement nestedOl = null;

      int mainCounter = 1;
      char nestedCounter = 'a';

      foreach (BodProgramu thisEntry in bodyProgramu)
      {
        if (thisEntry.SchvalovaniProgramu)
        {
          // Schvalování programu není v "programu" jako takovém.
          continue;
        }

        if (thisEntry.JePodbod)
        {
          if (nestedOl == null)
          {
            nestedCounter = 'a';
            nestedOl = mainOl.AppendElem("ol");
          }

          thisEntry.NadpisPoradi = $"{mainCounter - 1}{nestedCounter++}. ";
          nestedOl.AppendElem("li").InnerText = $"{thisEntry.NadpisPoradi}{thisEntry.Nadpis}{(thisEntry.JeDoplneny ? " (doplněný bod programu)" : string.Empty)}";
        }
        else
        {
          nestedOl = null;
          thisEntry.NadpisPoradi = $"{mainCounter++}. ";
          mainOl.AppendElem("li").InnerText = $"{thisEntry.NadpisPoradi}{thisEntry.Nadpis}{(thisEntry.JeDoplneny ? " (doplněný bod programu)" : string.Empty)}";
        }
      }
    }

    private string GenerateResolution(
      XmlElement body, Zasedani zas, BodProgramu programEntry, Usneseni resolution, int lastOrder, string replacementTitle = null)
    {
      string generatedResolutionTitle = null;

      if (!programEntry.SchvalovaniProgramu)
      {
        if (resolution.ZoBereNaVedomi)
        {
          body.AppendElem("p").InnerText = $"ZO {zas.NazevObce} bere na vědomí.";
        }
        else
        {
          generatedResolutionTitle = resolution.GenerateTitle(lastOrder + 1, zas);

          body.AppendElem("p").InnerText = "Návrh usnesení:";
          body.AppendElem("p").InnerText = generatedResolutionTitle;
        }
      }

      if (resolution.ZoBereNaVedomi)
      {
        return null;
      }

      IEnumerable<HlasovaniZastupitele> choiceFor = resolution.VolbyZastupitelu.Where(vol =>
        vol.Zastupitel.JePritomen &&
        vol.Volba == HlasovaniZastupitele.VolbaHlasovani.Pro);

      IEnumerable<HlasovaniZastupitele> choiceAgainst = resolution.VolbyZastupitelu.Where(vol =>
        vol.Zastupitel.JePritomen &&
        vol.Volba == HlasovaniZastupitele.VolbaHlasovani.Proti);

      IEnumerable<HlasovaniZastupitele> choiceDontKnow = resolution.VolbyZastupitelu.Where(vol =>
        vol.Zastupitel.JePritomen &&
        vol.Volba == HlasovaniZastupitele.VolbaHlasovani.ZdrzujeSe);

      string choiceForStr = choiceFor.Count() + (choiceFor.Any() ? $" ({string.Join(", ", choiceFor.Select(ch => ch.Zastupitel.Jmeno + " " + ch.Zastupitel.Prijmeni))})" : string.Empty);
      string choiceAgainstStr = choiceAgainst.Count() + (choiceAgainst.Any() ? $" ({string.Join(", ", choiceAgainst.Select(ch => ch.Zastupitel.Jmeno + " " + ch.Zastupitel.Prijmeni))})" : string.Empty);
      string choiceDontKnowStr = choiceDontKnow.Count() + (choiceDontKnow.Any() ? $" ({string.Join(", ", choiceDontKnow.Select(ch => ch.Zastupitel.Jmeno + " " + ch.Zastupitel.Prijmeni))})" : string.Empty);
      bool accepted = choiceFor.Count() > zas.Zastupitele.Count / 2;

      XmlElement div = body.AppendElem("div").AppendClass("resolution").AppendClass(accepted ? "success" : "failure");

      div.AppendElem("p").InnerText = $"{replacementTitle ?? "Hlasování o návrhu usnesení"}:";

      div.AppendElem("p").InnerText =
        $"<span class=\"resolution-vote resolution-success\">\u2713</span> PRO: {choiceForStr}<br/>" +
        $"<span class=\"resolution-vote resolution-failure\">\u00D7</span> PROTI: {choiceAgainstStr}<br/>" +
        $"<span class=\"resolution-vote resolution-dontknow\">?</span> ZDRŽUJE SE: {choiceDontKnowStr}";

      if (accepted)
      {
        div.AppendElem("p").InnerText = "<span class=\"resolution-success\">\u2713</span> Návrh byl přijat.";
      }
      else
      {
        div.AppendElem("p").InnerText = "<span class=\"resolution-failure\">\u00D7</span> Návrh nebyl přijat.";
      }

      return accepted ? generatedResolutionTitle : null;
    }

    private string GetStyle()
    {
      return File.ReadAllText(Path.Combine(Constants.PathsAndFiles.AppStylesFolder, "classic.css"));
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