using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ZZZO.Common
{
  public static class Utils
  {
    #region Metody

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

    public static string GetExecutingAssemblyFolder()
    {
      return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
    }

    public static FileVersionInfo GetExecutingAssemblyVersionInfo()
    {
      return FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
    }

    public static void HookKeyShortctut(
      Window window,
      object dataContext,
      KeyGesture keyGesture,
      MenuItem menuItem)
    {
      string commandName = menuItem.GetBindingExpression(MenuItem.CommandProperty).ParentBinding.Path.Path;
      var commandParameter = menuItem.GetBindingExpression(MenuItem.CommandParameterProperty)?.ParentBinding.ElementName;

      Binding bCommand = new Binding(commandName);
      bCommand.Source = dataContext;

      KeyBinding i = new KeyBinding(new RoutedCommand(), keyGesture);

      BindingOperations.SetBinding(
        i,
        InputBinding.CommandProperty,
        bCommand
      );

      if (!string.IsNullOrEmpty(commandParameter))
      {
        Binding bCommandParam = new Binding();
        bCommandParam.ElementName = commandParameter;

        BindingOperations.SetBinding(
          i,
          InputBinding.CommandParameterProperty,
          bCommandParam
        );
      }

      window.InputBindings.Add(i);
      menuItem.InputGestureText = keyGesture.DisplayString;
    }

    #endregion
  }
}