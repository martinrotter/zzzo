using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Win32;
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

      DpDenKonani.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
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

    private void UpdateVillageLogo(object sender, RoutedEventArgs e)
    {
      OpenFileDialog d = new OpenFileDialog();

      d.AddExtension = true;
      d.CheckPathExists = true;
      d.Filter = "Obrázky (BMP, PNG, JPG)|*.bmp;*.png;*.jpg;*.jpeg";
      d.Title = "Zvolte lokaci pro načtení loga ze souboru";

      if (d.ShowDialog().GetValueOrDefault())
      {
        App.Current.Zasedani.LogoObceData = File.ReadAllBytes(d.FileName);
      }
    }

    #endregion
  }
}