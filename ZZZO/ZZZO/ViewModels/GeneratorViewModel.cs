using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using ZZZO.Commands;
using ZZZO.Common.Generators;

namespace ZZZO.ViewModels
{
  public class GeneratorViewModel : ViewModelBase
  {
    #region Proměnné

    private bool _canGenerateOutputs;

    private byte[] _generatedData;

    private int _generateProgress;

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

    #endregion

    #region Konstruktory

    public GeneratorViewModel(ZzzoCore core)
    {
      Core = core;
      GenerateDocumentCmd = new RelayCommandEmpty(GenerateDocument, () => GenerateProgress <= 0);
      ExportHtmlCmd = new RelayCommandEmpty(ExportHtml, () => GenerateProgress <= 0 && GeneratedHtml != null);
      ExportPdfCmd = new RelayCommand<ChromiumWebBrowser>(ExportPdf, browser => GenerateProgress <= 0 && GeneratedHtml != null);
      PrintCmd = new RelayCommand<ChromiumWebBrowser>(PrintOnPrinter, browser => GenerateProgress <= 0 && GeneratedHtml != null);
    }

    #endregion

    #region Metody

    private string ChooseExportFile(string fileSuffix)
    {
      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{fileSuffix.ToUpper()} soubory (*.{fileSuffix})|*.{fileSuffix}";
      d.Title = $"Zvolte lokaci pro uložení zápisu zasedání do {fileSuffix.ToUpper()} souboru";

      if (!string.IsNullOrWhiteSpace(Core.Zasedani.VystupniSoubor))
      {
        d.FileName = App.Current.Core.Zasedani.VystupniSoubor + $".{fileSuffix}";
      }

      if (d.ShowDialog().GetValueOrDefault())
      {
        string chosenDir = Path.GetDirectoryName(d.FileName);
        string chosenFile = Path.GetFileNameWithoutExtension(d.FileName);

        Core.Zasedani.VystupniSoubor = Path.Combine(chosenDir, chosenFile);

        return d.FileName;
      }
      else
      {
        return null;
      }
    }

    private void ExportHtml()
    {
      string html = GeneratedHtml;
      string file = ChooseExportFile("html");

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
      string file = ChooseExportFile("pdf");

      if (!string.IsNullOrWhiteSpace(file))
      {
        try
        {
          await browser.PrintToPdfAsync(file, new PdfPrintSettings
          {
            DisplayHeaderFooter = false,
            Landscape = false,
            PrintBackground = false,
            PreferCssPageSize = true
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
      GenerateWithGenerator(GeneratorHtml);
    }

    private async void GenerateWithGenerator(Generator generator)
    {
      IProgress<int> prog = new Progress<int>(progress => { GenerateProgress = progress; });

      Task<byte[]> tsk = generator.Generate(Core.Zasedani, prog);

      try
      {
        await tsk;
        Generated(generator, tsk.Result);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Chyba při generování zápisu: {ex.Message}.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
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