﻿using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CefSharp;
using CefSharp.Wpf;
using ZZZO.Commands;
using ZZZO.Common;
using ZZZO.Common.Generators;
using static ZZZO.Common.Generators.Generator;
using EnumConverter = ZZZO.Converters.EnumConverter;

namespace ZZZO.ViewModels
{
  public class GeneratorViewModel : ViewModelBase
  {
    #region Proměnné

    private bool _canGenerateOutputs;

    private byte[] _generatedData;

    private int _generateProgress;
    private string _selectedHtmlStyle;
    private TypDokumentu _selectedKindOfDocument;

    #endregion

    #region Vlastnosti

    public bool CanGenerateOutputs
    {
      get => _canGenerateOutputs;
      set
      {
        if (value == _canGenerateOutputs)
        {
          return;
        }

        _canGenerateOutputs = value;
        OnPropertyChanged();
      }
    }

    public ZzzoCore Core
    {
      get;
    }

    public ICommand ExportHtmlCmd
    {
      get;
    }

    public ICommand ExportPdfCmd
    {
      get;
    }

    public byte[] GeneratedData
    {
      get => _generatedData;
      set
      {
        if (Equals(value, _generatedData))
        {
          return;
        }

        _generatedData = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(GeneratedHtml));
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public string GeneratedHtml
    {
      get => GeneratedData == null ? null : Encoding.UTF8.GetString(GeneratedData);
    }

    public ICommand GenerateDocumentCmd
    {
      get;
    }

    public int GenerateProgress
    {
      get => _generateProgress;
      set
      {
        if (value == _generateProgress)
        {
          return;
        }

        _generateProgress = value;
        OnPropertyChanged();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public GeneratorHtml GeneratorHtml
    {
      get;
    } = new GeneratorHtml();

    public ICommand PrintCmd
    {
      get;
    }

    public string SelectedHtmlFooterStyle
    {
      get => $"{SelectedHtmlStyle}.footer";
    }

    public TypDokumentu SelectedKindOfDocument
    {
      get
      {
        if (_selectedKindOfDocument == default)
        {
          _selectedKindOfDocument = GeneratorHtml.KindsOfDocuments.First();
        }

        return _selectedKindOfDocument;
      }

      set
      {
        if (value == _selectedKindOfDocument)
        {
          return;
        }

        _selectedKindOfDocument = value;
        OnPropertyChanged();
      }
    }

    public string SelectedHtmlStyle
    {
      get
      {
        if (!string.IsNullOrEmpty(Core?.Zasedani?.HtmlStyle))
        {
          _selectedHtmlStyle = Core.Zasedani.HtmlStyle;
        }
        else if (string.IsNullOrEmpty(_selectedHtmlStyle))
        {
          _selectedHtmlStyle = GeneratorHtml.Styles.First();
        }

        return _selectedHtmlStyle;
      }

      set
      {
        if (value == _selectedHtmlStyle)
        {
          return;
        }

        Core.Zasedani.HtmlStyle = value;
        _selectedHtmlStyle = value;

        OnPropertyChanged();
      }
    }

    #endregion

    #region Konstruktory

    public GeneratorViewModel(ZzzoCore core)
    {
      Core = core;

      Core.PropertyChanged += (sender, args) =>
      {
        if (args.PropertyName == nameof(ZzzoCore.Zasedani))
        {
          OnPropertyChanged(nameof(SelectedHtmlStyle));
        }
      };

      GenerateDocumentCmd = new RelayCommandEmpty(GenerateDocument, () => GenerateProgress <= 0);
      ExportHtmlCmd = new RelayCommandEmpty(ExportHtml, () => GenerateProgress <= 0 && GeneratedHtml != null);
      ExportPdfCmd = new RelayCommand<ChromiumWebBrowser>(ExportPdf, browser => GenerateProgress <= 0 && GeneratedHtml != null);
      PrintCmd = new RelayCommand<ChromiumWebBrowser>(PrintOnPrinter, browser => GenerateProgress <= 0 && GeneratedHtml != null);
    }

    #endregion

    #region Metody

    private void ExportHtml()
    {
      string html = GeneratedHtml;
      string suffix = EnumConverter.GetEnumDescription(SelectedKindOfDocument).ToLower();
      string file = ZzzoCore.ChooseSaveFile(Core.Zasedani, "html", true, suffix);

      if (!string.IsNullOrWhiteSpace(file))
      {
        try
        {
          File.WriteAllBytes(file, Encoding.UTF8.GetBytes(html));
        }
        catch (Exception ex)
        {
          MessageBox.Show($"HTML soubor nelze uložit: {ex.Message}.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    private async void ExportPdf(ChromiumWebBrowser browser)
    {
      string suffix = EnumConverter.GetEnumDescription(SelectedKindOfDocument).ToLower();
      string file = ZzzoCore.ChooseSaveFile(Core.Zasedani, "pdf", true, suffix);

      if (!string.IsNullOrWhiteSpace(file))
      {
        try
        {
          await browser.PrintToPdfAsync(file, new PdfPrintSettings
          {
            DisplayHeaderFooter = true,
            Landscape = false,
            PrintBackground = true,
            PreferCssPageSize = true,
            HeaderTemplate = "<div class='text center'></div>",
            FooterTemplate = GeneratorHtml.GetStyle(SelectedHtmlFooterStyle)
          });
        }
        catch (Exception ex)
        {
          MessageBox.Show($"PDF soubor nelze uložit: {ex.Message}.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    private void Generated(Generator generator, byte[] generatedData)
    {
      GeneratedData = generatedData;
    }

    private void GenerateDocument()
    {
      GenerateWithGenerator(GeneratorHtml, new GeneratorHtmlParams
      {
        HtmlStyle = SelectedHtmlStyle,
        KindOfDocument = SelectedKindOfDocument
      });
    }

    private async void GenerateWithGenerator(Generator generator, object param = null)
    {
      IProgress<int> prog = new Progress<int>(progress => { GenerateProgress = progress; });

      Task<byte[]> tsk = generator.Generate(Core.Zasedani, prog, param);

      try
      {
        await tsk;
        Generated(generator, tsk.Result);
      }
      catch (Exception ex)
      {
        MessageBox.Show(
          $"Chyba při generování dokumentu: {ex.Message}.",
          "Chyba",
          MessageBoxButton.OK, MessageBoxImage.Error);

        Generated(generator, null);
      }
      finally
      {
        prog.Report(0);
      }
    }

    private void PrintOnPrinter(ChromiumWebBrowser browser)
    {
      try
      {
        browser.Print();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Chyba při tisku: {ex.Message}.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    #endregion
  }
}