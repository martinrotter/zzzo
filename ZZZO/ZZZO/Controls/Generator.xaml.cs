using System.Windows.Controls;

namespace ZZZO.Controls;

public partial class Generator : UserControl
{
  public Generator()
  {
    InitializeComponent();
    DataContextChanged += (_, e) =>
    {
      if (e.OldValue is ViewModels.GeneratorViewModel old)
      { old.ZobrazitNahledAsync = null; old.VymazatNahled = null; old.Tisknout = null; }
      if (e.NewValue is ViewModels.GeneratorViewModel current)
      { current.ZobrazitNahledAsync = Nahled.ZobrazitAsync; current.VymazatNahled = Nahled.Vymazat; current.Tisknout = Nahled.Tisknout; }
    };
  }
}
