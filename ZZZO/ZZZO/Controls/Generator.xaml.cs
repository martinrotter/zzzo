using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using Microsoft.Win32;
using ZZZO.Common;

namespace ZZZO.Controls
{
  public partial class Generator : UserControl
  {
    #region Konstruktory

    public Generator()
    {
      InitializeComponent();
    }

    #endregion

    #region Metody

    public void NewDataGenerated(byte[] data)
    {
      WebBrowser.LoadHtml(data == null ? "" : Encoding.UTF8.GetString(data), Constants.Uris.Document);
    }

    private string ChooseExportFile(string fileSuffix)
    {
      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{fileSuffix.ToUpper()} soubory (*.{fileSuffix})|*.{fileSuffix}";
      d.Title = $"Zvolte lokaci pro uložení zápisu zasedání do {fileSuffix.ToUpper()} souboru";

      // Přesunout do viewmodelu? předavat parametr přes commandparameter
      if (!string.IsNullOrWhiteSpace(App.Current.Core.Zasedani.VystupniSoubor))
      {
        d.FileName = App.Current.Core.Zasedani.VystupniSoubor + $".{fileSuffix}";
      }

      if (d.ShowDialog().GetValueOrDefault())
      {
        string chosenDir = Path.GetDirectoryName(d.FileName);
        string chosenFile = Path.GetFileNameWithoutExtension(d.FileName);
          
        App.Current.Core.Zasedani.VystupniSoubor = Path.Combine(chosenDir, chosenFile);

        return d.FileName;
      }
      else
      {
        return null;
      }
    }

    private void ExportHtml(object sender, RoutedEventArgs e)
    {
      string html = WebBrowser.GetSourceAsync().Result;
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

    private async void ExportPdf(object sender, RoutedEventArgs e)
    {
      string file = ChooseExportFile("pdf");

      if (!string.IsNullOrWhiteSpace(file))
      {
        try
        {
          await WebBrowser.PrintToPdfAsync(file, new PdfPrintSettings
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

    private void PrintOnPrinter(object sender, RoutedEventArgs e)
    {
      try
      {
        WebBrowser.Print();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Chyba při tisku: {ex.Message}.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    #endregion
  }
}