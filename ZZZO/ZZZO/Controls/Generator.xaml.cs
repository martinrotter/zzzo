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

    private void Generated(Common.Generators.Generator generator, byte[] docxData)
    {
      SaveFileDialog d = new SaveFileDialog();

      d.OverwritePrompt = true;
      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = $@"{generator.FileSuffix.ToUpper()} soubory (*.{generator.FileSuffix})|*.{generator.FileSuffix}";
      d.Title = $"Zvolte lokaci pro uložení zápisu zasedání do {generator.FileSuffix.ToUpper()} souboru";
      
      if (!string.IsNullOrWhiteSpace(App.Current.Zasedani.VystupniSoubor))
      {
        d.FileName = App.Current.Zasedani.VystupniSoubor;
      }

      if (d.ShowDialog().GetValueOrDefault())
      {
        App.Current.Zasedani.VystupniSoubor = d.FileName;
        File.WriteAllBytes(d.FileName, docxData);
      }
    }

    private void GenerateDocument(object sender, RoutedEventArgs e)
    {
      Common.Generators.Generator generator = ((Button)e.Source).DataContext as Common.Generators.Generator;

      GenerateWithGenerator(generator);
    }

    private async void GenerateWithGenerator(Common.Generators.Generator generator)
    {
      IProgress<int> prog = new Progress<int>(progress => { PbGenerator.Value = progress; });

      Task<byte[]> tsk = generator.Generate(App.Current.Zasedani, prog);

      await tsk;

      prog.Report(0);
      Generated(generator, tsk.Result);
    }

    #endregion
  }
}