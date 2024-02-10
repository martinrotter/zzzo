using System.IO;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using ZZZO.Common;

namespace ZZZO.Controls
{
  public class TinyMceEditor : ChromiumWebBrowser
  {
    #region Statické prvky

    public static readonly DependencyProperty HtmlContentProperty = DependencyProperty.Register(
      nameof(HtmlContent),
      typeof(string),
      typeof(TinyMceEditor),
      new PropertyMetadata
      {
        PropertyChangedCallback = OnHtmlContentChanged
      }
    );

    #endregion

    #region Vlastnosti

    public string HtmlContent
    {
      get => (string)GetValue(HtmlContentProperty);
      set => SetValue(HtmlContentProperty, value);
    }

    private bool ChangingHtmlContent
    {
      get;
      set;
    }

    #endregion

    #region Konstruktory

    public TinyMceEditor()
    {
      Address = Path.Combine(Constants.PathsAndFiles.AppTinyMceFolder, "editor.html");
      JavascriptMessageReceived += OnJavascriptMessageReceived;
    }

    #endregion

    #region Metody

    private static void OnHtmlContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      TinyMceEditor editor = d as TinyMceEditor;

      if (editor == null || editor.ChangingHtmlContent)
      {
        return;
      }

      string newHtml = e.NewValue as string;

      editor.SetHtmlContent(newHtml);
    }

    public void SetHtmlContent(string html)
    {
      if (CanExecuteJavascriptInMainFrame)
      {
        this.EvaluateScriptAsync("setEditorContent", html);
      }
    }

    private void OnJavascriptMessageReceived(object sender, JavascriptMessageReceivedEventArgs e)
    {
      Dispatcher.Invoke(() =>
      {
        ChangingHtmlContent = true;
        HtmlContent = e.Message as string;
        ChangingHtmlContent = false;
      });
    }

    #endregion
  }
}