using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace ZZZO.Converters
{
  [ValueConversion(typeof(Enum), typeof(string))]
  public class EnumConverter : IValueConverter
  {
    #region Implementace rozhraní

    public object Convert(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      string description = GetEnumDescription((Enum)value);

      return description;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
      CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion

    #region Metody

    private static string GetEnumDescription(Enum value)
    {
      FieldInfo fi = value.GetType().GetField(value.ToString());

      if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any())
      {
        return attributes.First().Description;
      }

      return value.ToString();
    }

    #endregion
  }
}