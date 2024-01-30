using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZZZO.Common.API;

namespace ZZZO.Controls
{
  /// <summary>
  /// Interaction logic for BasicCityInfo.xaml
  /// </summary>
  public partial class BasicCityInfo : UserControl
  {
    #region Konstruktory

    public BasicCityInfo()
    {
      InitializeComponent();
    }

    #endregion

    #region Metody

    private void AddZastupitel(object sender, RoutedEventArgs e)
    {
      App.Current.Zasedani.AddZastupitel(new Zastupitel
      {
        Jmeno = "Pepa",
        Prijmeni = "Z Depa"
      });
    }

    private void KeyPressedInListOfZastupitele(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        RemoveZastupitel(this, e);
      }
    }

    private void RemoveZastupitel(object sender, RoutedEventArgs e)
    {
      if (LvZastupitele.SelectedValue is Zastupitel zast)
      {
        App.Current.Zasedani.RemoveZastupitel(zast);
      }
    }

    #endregion
  }
}