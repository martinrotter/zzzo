using System;
using System.Globalization;
using System.Windows.Data;

namespace ZZZO.Converters
{
  [ValueConversion(typeof(object), typeof(bool))]
  public class NotNullConverter : IValueConverter
  {
    #region Implementace rozhraní

    public object Convert(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}