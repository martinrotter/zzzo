#nullable enable

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ZZZO.Extensions
{
  public static class DataGridExtensions
  {
    #region DefaultSorting

    public static readonly DependencyProperty DefaultSortingProperty =
      DependencyProperty.RegisterAttached(
        nameof(DefaultSortingProperty).Replace("Property", string.Empty),
        typeof(string),
        typeof(DataGridExtensions),
        new PropertyMetadata(string.Empty, OnDefaultSortingChanged));

    public static string? GetDefaultSorting(DependencyObject element)
    {
      return (string?)element.GetValue(DefaultSortingProperty);
    }

    public static void SetDefaultSorting(DependencyObject element, string? value)
    {
      element.SetValue(DefaultSortingProperty, value);
    }

    private static void OnDefaultSortingChanged(
      DependencyObject element,
      DependencyPropertyChangedEventArgs args)
    {
      if (element is not DataGrid dataGrid)
      {
        throw new ArgumentException("Element should be DataGrid.");
      }

      if (args.NewValue is not string sorting)
      {
        throw new ArgumentException("Type should be string.");
      }

      string[] values = sorting.Split(':');

      if (values.Length != 2)
      {
        throw new InvalidOperationException("String should be like 'A:Name' or 'D:Name'.");
      }

      dataGrid.Items.SortDescriptions.Clear();

      dataGrid.Items.SortDescriptions.Add(
        new SortDescription(
          values[1],
          values[0] == "D"
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending));
    }

    #endregion
  }
}