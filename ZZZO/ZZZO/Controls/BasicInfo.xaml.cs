using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
using ZZZO.Common.API;

namespace ZZZO.Controls
{
  public partial class BasicInfo : UserControl
  {
    #region Konstruktory

    public BasicInfo()
    {
      InitializeComponent();
      System.Windows.Controls.Validation.AddErrorHandler(this, (_, e) =>
      {
        if (DataContext is not ViewModels.BasicInfoViewModel vm) return;
        var vazba = e.Error.BindingInError as System.Windows.Data.BindingExpression;
        string pole = vazba?.ParentBinding.Path.Path?.Split('.').LastOrDefault();
        string nazev = pole == nameof(Zasedani.Poradi) ? "Pořadí zasedání" : pole == nameof(Zasedani.PocetHostu) ? "Počet hostů" : "Hodnota";
        vm.Core.ZmenitChybuVstupu(e.Error, e.Action == ValidationErrorEventAction.Added
          ? new Common.Validace.ChybaValidace("vstup", nazev + ": zadejte platné celé číslo.", Common.Validace.OblastValidace.ZakladniUdaje, Vlastnost: pole) : null);
      });

      DpCasKonani.Language = DpDenKonani.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
    }

    #endregion
  }
}
