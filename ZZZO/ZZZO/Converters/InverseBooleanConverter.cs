using System;
using System.Globalization;
using System.Windows.Data;

namespace ZZZO.Converters
{
  [ValueConversion(typeof(bool), typeof(bool))]
  public class InverseBooleanConverter : IValueConverter
  {
    #region IValueConverter Members

    public object Convert(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      if (targetType != typeof(bool))
      {
        throw new InvalidOperationException("The target must be a boolean");
      }

      if (value is double d)
      {
        return d == 0.0;
      }

      return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}