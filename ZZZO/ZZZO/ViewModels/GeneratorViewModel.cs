using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.Generators;
using Generator = ZZZO.Controls.Generator;

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
      }
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
        CommandManager.InvalidateRequerySuggested();
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
      GenerateDocumentCmd = new RelayCommand(GenerateDocument, obj => GenerateProgress <= 0);
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

    #endregion
  }
}