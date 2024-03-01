using System.IO;
using System.Net.Http;
using HtmlAgilityPack;
using Microsoft.Win32;
using ZZZO.Common;
using ZZZO.Common.API;
using ZZZO.ViewModels;

namespace ZZZO
{
  public class ZzzoCore : ObservableObject
  {
    #region Proměnné

    private Zasedani _zasedani;

    #endregion

    #region Vlastnosti

    public Zasedani Zasedani
    {
      get => _zasedani;
      set
      {
        if (Equals(value, _zasedani))
        {
          return;
        }

        _zasedani = value;

        ZasedaniOriginalData = _zasedani.ToJson();

        OnPropertyChanged();
        OnPropertyChanged(nameof(ZasedaniLoaded));
      }
    }

    public bool ZasedaniIsDirty
    {
      get => ZasedaniLoaded && ZasedaniOriginalData != null && !Zasedani.ToJson().SequenceEqual(ZasedaniOriginalData);
    }

    public bool ZasedaniLoaded
    {
      get => Zasedani != null;
    }

    /// <summary>
    /// Is used to determine if there is any change or not.
    /// </summary>
    private byte[] ZasedaniOriginalData
    {
      get;
      set;
    }

    #endregion

    #region Metody

    public static string ChooseLoadFile(string fileSuffix, string title = null)
    {
      OpenFileDialog d = new OpenFileDialog();

      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{fileSuffix.ToUpper()} soubory (*.{fileSuffix})|*.{fileSuffix}";
      d.Title = title ?? $"Zvolte lokaci pro načtení zápisu zasedání ze {fileSuffix.ToUpper()} souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        return d.FileName;
      }
      else
      {
        return null;
      }
    }

    public static string ChooseSaveFile(
      Zasedani zasedani,
      string fileSuffix,
      bool forceChooseFile,
      string title = null)
    {
      string initialFullPath = (zasedani?.VystupniSoubor ?? string.Empty) + $".{fileSuffix}";

      if (!forceChooseFile && File.Exists(initialFullPath))
      {
        // We do not have to ask user.
        return initialFullPath;
      }

      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{fileSuffix.ToUpper()} soubory (*.{fileSuffix})|*.{fileSuffix}";
      d.Title = title ?? $"Zvolte lokaci pro uložení zápisu zasedání do {fileSuffix.ToUpper()} souboru";

      if (zasedani != null && !string.IsNullOrWhiteSpace(zasedani.VystupniSoubor))
      {
        string initialDirectory = Path.GetDirectoryName(zasedani.VystupniSoubor);
        string initialFileName = Path.GetFileName(zasedani.VystupniSoubor) + $".{fileSuffix}";

        d.InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        d.FileName = initialFileName;
      }

      if (d.ShowDialog().GetValueOrDefault())
      {
        if (zasedani != null)
        {
          string chosenDir = Path.GetDirectoryName(d.FileName);
          string chosenFile = Path.GetFileNameWithoutExtension(d.FileName);

          zasedani.VystupniSoubor = Path.Combine(chosenDir, chosenFile);
        }

        return d.FileName;
      }
      else
      {
        return null;
      }
    }

    public Task<IEnumerable<CityLogo>> DownloadCityLogos(string cityName)
    {
      return Task.Run<IEnumerable<CityLogo>>(() =>
      {
        if (string.IsNullOrWhiteSpace(cityName))
        {
          throw new Exception("je třeba zadat název obce");
        }

        HttpClient cl = new HttpClient();
        string baseUrl = $"{Constants.Uris.RekosBase}/vyhledani-symbolu?obec={cityName}&sort=municipality.name&page=";
        int pageNumber = 1;

        string mainHtml = cl
          .GetStringAsync(baseUrl + pageNumber++)
          .Result;

        List<CityLogo> logos = new List<CityLogo>();
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(mainHtml);
        logos.AddRange(ExtractLogosFromPage(doc));

        HtmlNodeCollection pages = doc.DocumentNode.SelectNodes("//span[@class='pages']/a");

        if (pages != null)
        {
          List<Task<List<CityLogo>>> subTasks = new List<Task<List<CityLogo>>>();

          foreach (HtmlNode page in pages)
          {
            if (!int.TryParse(page.InnerHtml, out int pageNum) ||
                pageNum == 1 ||
                page.Attributes.Contains("class"))
            {
              continue;
            }

            if (pageNum != pageNumber)
            {
              break;
            }

            int actualPageNumber = pageNumber++;

            subTasks.Add(Task.Run(() =>
            {
              string pageUrl = baseUrl + actualPageNumber;
              string pageHtml = cl.GetStringAsync(pageUrl).Result;
              HtmlDocument pageDoc = new HtmlDocument();
              pageDoc.LoadHtml(pageHtml);
              return ExtractLogosFromPage(pageDoc);
            }));
          }

          foreach (List<CityLogo> pagedLogos in Task.WhenAll(subTasks).Result)
          {
            logos.AddRange(pagedLogos);
          }
        }

        return logos;
      });
    }

    public Task<CityLogo> DownloadFullCityLogo(CityLogo cityLogo)
    {
      return Task.Run(() =>
      {
        using HttpClient cl = new HttpClient();
        cityLogo.LogoObceData = cl.GetByteArrayAsync(cityLogo.LogoObceUrl).Result;
        return cityLogo;
      });
    }

    public bool LoadZasedani()
    {
      string filePath = ChooseLoadFile(Constants.PathsAndFiles.ZzzoFileSuffix);

      if (File.Exists(filePath))
      {
        Zasedani = Zasedani.LoadFromFile(filePath);
        return true;
      }
      else
      {
        return false;
      }
    }

    public void NewZaseDani(Zasedani zas)
    {
      Zasedani = zas;
    }

    public bool SaveZasedani(bool forceChooseFile)
    {
      if (Zasedani == null)
      {
        return false;
      }

      string soubor = ChooseSaveFile(Zasedani, Constants.PathsAndFiles.ZzzoFileSuffix, forceChooseFile);

      if (!string.IsNullOrEmpty(soubor))
      {
        Zasedani.SaveToFile(soubor);
        ZasedaniOriginalData = Zasedani.ToJson();
        return true;
      }
      else
      {
        return false;
      }
    }

    private List<CityLogo> ExtractLogosFromPage(HtmlDocument doc)
    {
      HtmlNodeCollection rows = doc.DocumentNode.SelectNodes("//table[@class='zebra']/tbody/tr");

      if (rows == null)
      {
        return new List<CityLogo>();
      }

      List<CityLogo> tsks = new List<CityLogo>(rows.Count);

      foreach (HtmlNode row in rows)
      {
        string city = row.SelectSingleNode("td/a")?.InnerText;
        string outerCity = row.SelectSingleNode("td[2]")?.InnerText;
        HtmlNode logoMinElem = row.SelectSingleNode("td[3]/img");

        if (city == null || outerCity == null || logoMinElem == null)
        {
          continue;
        }

        string logoMinPath = logoMinElem.GetAttributeValue("src", string.Empty);
        string logoBigPath = logoMinPath.Replace("30x30", "800x500");

        CityLogo cla = new CityLogo
        {
          CityName = city,
          ExtendedCityClusterName = outerCity,
          LogoObceUrl = Constants.Uris.RekosBase + logoBigPath
        };

        tsks.Add(cla);
      }

      return tsks;
    }

    #endregion
  }
}