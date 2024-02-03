using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ZZZO.Converters
{
  [ValueConversion(typeof(bool), typeof(Visibility))]
  public class CollapseOnTrueConverter : IValueConverter
  {
    #region Implementace rozhraní

    public object Convert(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      if (value is bool display)
      {
        if (parameter is true)
        {
          display = !display;
        }

        return display ? Visibility.Collapsed : Visibility.Visible;
      }

      return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}