using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

    private async void Generate(Common.Generators.Generator generator)
    {
      Progress<int> prog = new Progress<int>(progress => { PbGenerator.Value = progress; });

      Task<byte[]> tsk = generator.Generate(App.Current.Zasedani, prog);

      await tsk;

      Generated(generator, tsk.Result);
    }

    private void Generated(Common.Generators.Generator generator, byte[] docxData)
    {
      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{generator.FileSuffix.ToUpper()} soubory (*.{generator.FileSuffix})|*.{generator.FileSuffix}";
      d.Title = $"Zvolte lokaci pro uložení zápisu zasedání do {generator.FileSuffix.ToUpper()} souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        File.WriteAllBytes(d.FileName, docxData);
      }
    }

    private async void GenerateDocx(object sender, RoutedEventArgs e)
    {
      Generate(App.Current.GeneratorDocx);
    }

    private void GenerateHtml(object sender, RoutedEventArgs e)
    {
      Generate(App.Current.GeneratorHtml);
    }

    #endregion
  }
}