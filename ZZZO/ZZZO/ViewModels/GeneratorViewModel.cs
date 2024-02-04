using System;
using System.Threading.Tasks;
using ZZZO.Commands;
using ZZZO.Common;
using ZZZO.Common.Generators;
using Generator = ZZZO.Controls.Generator;

namespace ZZZO.ViewModels
{
  public class GeneratorViewModel : ViewModelBase
  {
    #region Proměnné

    private int _generateProgress;

    #endregion

    #region Vlastnosti

    public ZzzoCore Core
    {
      get;
    }

    public byte[] GeneratedData
    {
      get;
      set;
    }

    public RelayCommand GenerateDocumentCmd
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
      }
    }

    public GeneratorHtml GeneratorHtml
    {
      get;
    } = new GeneratorHtml();

    #endregion

    #region Konstruktory

    public GeneratorViewModel(Generator view, ZzzoCore core)
    {
      // View is only used to bind necessary events.
      NewDataGenerated += view.NewDataGenerated;

      Core = core;
      GenerateDocumentCmd = new RelayCommand(GenerateDocument, IsNotGeneratingDocument);
    }

    #endregion

    #region Metody

    public event Action<byte[]> NewDataGenerated;

    private void Generated(Common.Generators.Generator generator, byte[] generatedData)
    {
      GeneratedData = generatedData;
      NewDataGenerated?.Invoke(GeneratedData);
    }

    private void GenerateDocument(object obj)
    {
      GenerateWithGenerator(GeneratorHtml);
    }

    private async void GenerateWithGenerator(Common.Generators.Generator generator)
    {
      IProgress<int> prog = new Progress<int>(progress => { GenerateProgress = progress; });

      Task<byte[]> tsk = generator.Generate(App.Current.Zasedani, prog);

      await tsk;

      prog.Report(0);
      Generated(generator, tsk.Result);
    }

    private bool IsNotGeneratingDocument(object arg)
    {
      return !GeneratorHtml.IsGenerating;
    }

    #endregion
  }
}