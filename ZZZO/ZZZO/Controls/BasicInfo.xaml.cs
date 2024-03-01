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

      DpCasKonani.Language = DpDenKonani.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
    }

    #endregion
  }
}