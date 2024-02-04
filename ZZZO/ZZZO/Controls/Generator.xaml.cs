using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using Microsoft.Win32;

namespace ZZZO.Controls
{
  /// <summary>
  /// Interaction logic for Generator.xaml
  /// </summary>
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
      WebBrowser.LoadHtml(Encoding.UTF8.GetString(data), "https://document.zzzo");
    }

    private string ChooseExportFile(string fileSuffix)
    {
      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{fileSuffix.ToUpper()} soubory (*.{fileSuffix})|*.{fileSuffix}";
      d.Title = $"Zvolte lokaci pro uložení zápisu zasedání do {fileSuffix.ToUpper()} souboru";

      if (!string.IsNullOrWhiteSpace(App.Current.Zasedani.VystupniSoubor))
      {
        d.FileName = App.Current.Zasedani.VystupniSoubor;
      }

      if (d.ShowDialog().GetValueOrDefault())
      {
        App.Current.Zasedani.VystupniSoubor = d.FileName;
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
        File.WriteAllBytes(file, Encoding.UTF8.GetBytes(html));
      }
    }

    private void ExportPdf(object sender, RoutedEventArgs e)
    {
      string file = ChooseExportFile("pdf");

      if (!string.IsNullOrWhiteSpace(file))
      {
        WebBrowser.PrintToPdfAsync(file, new PdfPrintSettings
        {
          DisplayHeaderFooter = false,
          Landscape = false,
          PrintBackground = false,
          PreferCssPageSize = true
        });
      }
    }

    private void PrintOnPrinter(object sender, RoutedEventArgs e)
    {
      WebBrowser.Print();
    }

    #endregion
  }
}