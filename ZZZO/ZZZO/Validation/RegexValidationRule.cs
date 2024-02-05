using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace ZZZO.Validation
{
  public class RegexValidationRule : ValidationRule
  {
    #region Vlastnosti

    public string RegularExpression
    {
      get;
      set;
    }

    #endregion

    #region Metody

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
      /*
      if (value is BindingExpression expr)
      {
        value = Utils.GetBoundValue(value);

        if (value != null && value.GetType() != typeof(string))
        {
          value = value.ToString();
        }
      }
      */

      if (value is string str && Regex.IsMatch(str, RegularExpression))
      {
        return ValidationResult.ValidResult;
      }
      else
      {
        return new ValidationResult(false, "Text nesplňuje kritéria.");
      }
    }

    #endregion
  }
}