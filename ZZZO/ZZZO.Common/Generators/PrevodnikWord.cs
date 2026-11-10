using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ZZZO.Common.Generators;

/// <summary>Runs Word's desktop COM API in a separate STA helper, without a window.</summary>
public static class PrevodnikWord
{
  public const string Argument = "--zzzo-word-pdf";
  public static bool JeDostupny => Type.GetTypeFromProgID("Word.Application") != null;

  [DllImport("user32.dll")]
  private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

  // Called before WPF/CEF startup. Only an instance created by this helper is owned.
  public static int Spustit(string input, string output, string ownershipFile)
  {
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    dynamic word = null, documents = null, document = null, windows = null, window = null;
    bool owned = false;
    try
    {
      var existing = Process.GetProcessesByName("WINWORD");
      var existingIds = existing.Select(p => p.Id).ToHashSet();
      foreach (var process in existing) process.Dispose();
      Type type = Type.GetTypeFromProgID("Word.Application") ?? throw new IOException("Microsoft Word není nainstalovaný.");
      word = Activator.CreateInstance(type);
      documents = word.Documents;
      if ((int)documents.Count != 0)
        throw new IOException("Word neposkytl samostatnou prázdnou instanci pro převod.");
      owned = true;
      word.Visible = false;
      word.DisplayAlerts = 0;
      word.AutomationSecurity = 3; // msoAutomationSecurityForceDisable
      // Word exposes HWND on Window, not Application. A temporary hidden blank
      // document identifies the COM instance before opening the actual input.
      // Visible=false on Documents.Add suppresses Window creation entirely;
      // the application itself remains hidden while this probe has a Window.
      document = documents.Add();
      windows = document.Windows;
      window = windows.Item(1);
      GetWindowThreadProcessId(new IntPtr((int)window.Hwnd), out uint pid);
      if (pid == 0 || existingIds.Contains((int)pid))
      {
        owned = false;
        throw new IOException("Word neposkytl samostatnou prázdnou instanci pro převod.");
      }
      using (var process = Process.GetProcessById((int)pid))
        File.WriteAllLines(ownershipFile, new[] { pid.ToString(), process.StartTime.ToUniversalTime().Ticks.ToString() });
      document.Close(0);
      Marshal.FinalReleaseComObject(window); window = null;
      Marshal.FinalReleaseComObject(windows); windows = null;
      Marshal.FinalReleaseComObject(document); document = null;
      document = documents.Open(FileName: Path.GetFullPath(input), ConfirmConversions: false,
        ReadOnly: true, AddToRecentFiles: false, Visible: false);
      document.Repaginate();
      document.ExportAsFixedFormat(OutputFileName: Path.GetFullPath(output), ExportFormat: 17,
        OpenAfterExport: false, OptimizeFor: 0, CreateBookmarks: 1, DocStructureTags: true);
      return 0;
    }
    catch (Exception ex) { Console.Error.WriteLine("Microsoft Word: " + ex.Message); return 1; }
    finally
    {
      if (window != null) Marshal.FinalReleaseComObject(window);
      if (windows != null) Marshal.FinalReleaseComObject(windows);
      if (document != null)
      {
        try { document.Close(0); } catch (COMException) { }
        Marshal.FinalReleaseComObject(document);
      }
      if (documents != null) Marshal.FinalReleaseComObject(documents);
      if (word != null)
      {
        if (owned) { try { word.Quit(0); } catch (COMException) { } }
        Marshal.FinalReleaseComObject(word);
      }
    }
  }

  internal static async Task PrevestAsync(string input, string output, string work, CancellationToken cancellationToken)
  {
    if (!JeDostupny) throw new FileNotFoundException("Microsoft Word není nainstalovaný.");
    string helper = Path.Combine(AppContext.BaseDirectory, "ZZZO.exe");
    string ownershipFile = Path.Combine(work, "word-process.txt");
    var info = new ProcessStartInfo(helper)
    {
      UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
      RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = work
    };
    foreach (string arg in new[] { Argument, input, output, ownershipFile }) info.ArgumentList.Add(arg);
    using var process = Process.Start(info) ?? throw new IOException("Převodník Microsoft Word se nepodařilo spustit.");
    var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
    var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeout.CancelAfter(TimeSpan.FromSeconds(90));
    try
    {
      try { await process.WaitForExitAsync(timeout.Token); }
      catch (OperationCanceledException)
      {
        try { process.Kill(true); await process.WaitForExitAsync(CancellationToken.None); } catch (InvalidOperationException) { }
        cancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException("Převod pomocí Microsoft Wordu překročil 90 sekund.");
      }
      string diagnostics = ((await stdout) + Environment.NewLine + (await stderr)).Trim();
      if (process.ExitCode != 0 || !File.Exists(output)) throw new IOException("Microsoft Word nevytvořil PDF. " + diagnostics);
    }
    finally { UkliditVlastniInstanci(ownershipFile); }
  }

  private static void UkliditVlastniInstanci(string ownershipFile)
  {
    try
    {
      if (!File.Exists(ownershipFile)) return;
      string[] record = File.ReadAllLines(ownershipFile);
      if (record.Length != 2 || !int.TryParse(record[0], out int pid) || !long.TryParse(record[1], out long started)) return;
      using var process = Process.GetProcessById(pid);
      // A recycled PID or a user's pre-existing Word process must never be stopped.
      if (process.ProcessName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase)
        && process.StartTime.ToUniversalTime().Ticks == started && !process.WaitForExit(2000))
        process.Kill();
    }
    catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
  }
}
