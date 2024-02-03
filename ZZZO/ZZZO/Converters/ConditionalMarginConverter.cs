using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace ZZZO.Converters
{
  [ValueConversion(typeof(bool), typeof(Thickness))]
  public class ConditionalMarginConverter : IValueConverter
  {
    #region Implementace rozhraní

    public object Convert(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      if (value is bool b)
      {
        if (b)
        {
          return (Thickness)parameter;
        }
      }

      return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion

    #region Metody

    #endregion
  }
}