﻿using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using ZZZO.Common;

namespace ZZZO.Assists
{
  public class ChromiumWebBrowserAssist : DependencyObject
  {
    #region Statické prvky

    public static readonly DependencyProperty HtmlProperty =
      DependencyProperty.RegisterAttached(
        "Html",
        typeof(string),
        typeof(ChromiumWebBrowserAssist),
        new PropertyMetadata
        {
          PropertyChangedCallback = OnHtmlChanged
        });

    #endregion

    #region Metody

    public static string GetHtml(DependencyObject obj)
    {
      return (string)obj.GetValue(HtmlProperty);
    }

    public static void SetHtml(DependencyObject obj, string value)
    {
      obj.SetValue(HtmlProperty, value);
    }

    private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (e.Property.Name == HtmlProperty.Name && d is ChromiumWebBrowser browser)
      {
        if (e.NewValue is string html)
        {
          browser.LoadHtml(html, Constants.Uris.Document);
        }
        else
        {
          browser.LoadHtml("<p>Nevalidní HTML.</p>", Constants.Uris.Document);
        }
      }
    }

    #endregion
  }
}