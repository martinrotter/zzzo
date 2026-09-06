using System.Globalization;
using System.Windows.Data;
using ZZZO.Common.API;
using ZZZO.Common.Validace;
namespace ZZZO.Converters;

public class ValidaceConverter : IMultiValueConverter
{
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    if (values.Length < 2 || values[1] is not IEnumerable<ChybaValidace> chyby) return "";
    var identifikatory = new HashSet<Guid>();
    void PridatBod(BodProgramu bod)
    {
      identifikatory.Add(bod.Id);
      foreach (var u in bod.Usneseni) identifikatory.Add(u.Id);
      foreach (var podbod in bod.Podbody) PridatBod(podbod);
    }
    if (values[0] is Guid id) identifikatory.Add(id);
    else if (values[0] is BodProgramu bod) PridatBod(bod);
    var text = string.Join(Environment.NewLine, chyby.Where(c => c.PolozkaId.HasValue && identifikatory.Contains(c.PolozkaId.Value)).Select(c => c.Popis));
    return parameter as string == "Ikona" ? (text.Length > 0 ? "⚠" : "") : text;
  }
  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
