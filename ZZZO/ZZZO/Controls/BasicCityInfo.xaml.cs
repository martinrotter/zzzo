using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZZZO.Common.API;

namespace ZZZO.Controls
{
  /// <summary>
  /// Interaction logic for BasicCityInfo.xaml
  /// </summary>
  public partial class BasicCityInfo : UserControl
  {
    public BasicCityInfo()
    {
      InitializeComponent();
    }

    private void AddZastupitel(object sender, RoutedEventArgs e)
    {
      App.Current.Zasedani.Zastupitele.Add(new Zastupitel
      {
        Jmeno = "Pepa",
        Prijmeni = "Z Depa"
      });
    }

    private void RemoveZastupitel(object sender, RoutedEventArgs e)
    {
      if (LvZastupitele.SelectedValue is Zastupitel zast)
      {
        App.Current.Zasedani.Zastupitele.Remove(zast);
      }
    }

    private void KeyPressedInListOfZastupitele(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        RemoveZastupitel(this, e);
      }
    }
  }
}
