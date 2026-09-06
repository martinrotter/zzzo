using System.Windows.Input;
using ZZZO.Commands;
using ZZZO.Common.API;
namespace ZZZO.ViewModels;

public class ResolutionViewModel : ViewModelBase
{
  public ZzzoCore Core { get; }
  private Usneseni _usneseni;
  public Usneseni Usneseni { get => _usneseni; set { if (SetProperty(ref _usneseni, value)) Obnovit(); } }
  public VyhodnoceniHlasovani Vyhodnoceni => Usneseni == null || Core.Zasedani == null ? null : VyhodnoceniHlasovani.Vyhodnotit(Core.Zasedani, Usneseni);
  public ICommand AllAgreeCmd { get; }
  public ICommand AllDisagreeCmd { get; }
  public ICommand VsichniSeZdrzeliCmd { get; }
  public ResolutionViewModel(ZzzoCore core)
  {
    Core = core;
    AllAgreeCmd = new RelayCommand(_ => Nastavit(HlasovaniZastupitele.VolbaHlasovani.Pro), _ => Usneseni != null);
    AllDisagreeCmd = new RelayCommand(_ => Nastavit(HlasovaniZastupitele.VolbaHlasovani.Proti), _ => Usneseni != null);
    VsichniSeZdrzeliCmd = new RelayCommand(_ => Nastavit(HlasovaniZastupitele.VolbaHlasovani.ZdrzujeSe), _ => Usneseni != null);
  }
  private void Nastavit(HlasovaniZastupitele.VolbaHlasovani volba)
  {
    foreach (var hlas in Usneseni.VolbyZastupitelu.Where(h => h.Zastupitel?.JePritomen == true)) hlas.Volba = volba;
  }
  public void Obnovit() => OnPropertyChanged(nameof(Vyhodnoceni));
}
