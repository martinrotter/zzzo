using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace ZZZO.Common
{
  public static class Utils
  {
    #region Metody

    public static FileVersionInfo GetExecutingAssemblyVersionInfo()
    {
      return FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
    }

    public static object GetBoundValue(object value)
    {
      if (value is BindingExpression)
      {
        // ValidationStep was UpdatedValue or CommittedValue (validate after setting)
        // Need to pull the value out of the BindingExpression.
        BindingExpression binding = (BindingExpression)value;

        // Get the bound object and name of the property
        string resolvedPropertyName = binding.GetType().GetProperty("ResolvedSourcePropertyName", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).GetValue(binding, null)?.ToString();
        object resolvedSource = binding.GetType().GetProperty("ResolvedSource", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).GetValue(binding, null);

        if (resolvedSource == null || resolvedPropertyName == null)
        {
          return null;
        }

        // Extract the value of the property
        object propertyValue = resolvedSource.GetType().GetProperty(resolvedPropertyName).GetValue(resolvedSource, null);

        return propertyValue;
      }
      else
      {
        return value;
      }
    }

    #endregion
  }
}