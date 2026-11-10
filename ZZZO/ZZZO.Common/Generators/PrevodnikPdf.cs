using System.Diagnostics;
using System.IO;

namespace ZZZO.Common.Generators;

/// <summary>PDF is always a conversion of the exact DOCX offered for export.</summary>
public sealed class PrevodnikPdf
{
  private readonly Func<string, string, string, CancellationToken, Task> _word;
  public string PouzityPrevodnik { get; private set; }
  public PrevodnikPdf() : this(PrevodnikWord.PrevestAsync) { }
  internal PrevodnikPdf(Func<string, string, string, CancellationToken, Task> word) => _word = word;

  public static string NajitLibreOffice()
  {
    string configured = Environment.GetEnvironmentVariable("ZZZO_LIBREOFFICE");
    if (!string.IsNullOrWhiteSpace(configured))
      return File.Exists(configured) ? configured : throw new FileNotFoundException("Cesta ZZZO_LIBREOFFICE neukazuje na existující soffice.exe.", configured);
    var roots = new[] { Path.Combine(AppContext.BaseDirectory, "LibreOffice", "program"),
      Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreOffice", "program"),
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreOffice", "program"),
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "LibreOffice", "program") }
      .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator));
    return roots.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => Path.Combine(r.Trim('"'), "soffice.exe")).FirstOrDefault(File.Exists);
  }

  public async Task<byte[]> PrevestAsync(byte[] docx, CancellationToken cancellationToken = default)
  {
    PouzityPrevodnik = null;
    string work = Path.Combine(Path.GetTempPath(), "ZZZO-PDF-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(work);
    try
    {
      string input = Path.Combine(work, "dokument.docx");
      string output = Path.Combine(work, "dokument.pdf");
      await File.WriteAllBytesAsync(input, docx, cancellationToken);
      string wordError = null;
      if (!string.Equals(Environment.GetEnvironmentVariable("ZZZO_PDF_CONVERTER"), "libreoffice", StringComparison.OrdinalIgnoreCase))
      {
        try
        {
          await _word(input, output, work, cancellationToken);
          byte[] pdf = await NacistPdfAsync(output, cancellationToken);
          PouzityPrevodnik = "Microsoft Word";
          return pdf;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
          wordError = ex.Message;
          if (File.Exists(output)) File.Delete(output);
        }
      }
      try
      {
        await PrevestLibreOfficeAsync(input, output, work, cancellationToken);
        byte[] pdf = await NacistPdfAsync(output, cancellationToken);
        PouzityPrevodnik = "LibreOffice";
        return pdf;
      }
      catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
      {
        throw new IOException("PDF se nepodařilo vytvořit. DOCX lze exportovat i bez převodníku. " +
          (wordError == null ? "" : "Microsoft Word: " + wordError + " ") + "LibreOffice: " + ex.Message, ex);
      }
    }
    finally
    {
      // Only this invocation's newly created, absolute temp directory is removed.
      try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
  }

  private static async Task<byte[]> NacistPdfAsync(string output, CancellationToken cancellationToken)
  {
    byte[] pdf = await File.ReadAllBytesAsync(output, cancellationToken);
    if (pdf.Length < 5 || !pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) throw new InvalidDataException("Převodník vrátil neplatné PDF.");
    return pdf;
  }

  private static async Task PrevestLibreOfficeAsync(string input, string output, string work, CancellationToken cancellationToken)
  {
      string executable = NajitLibreOffice() ?? throw new FileNotFoundException("Nainstalujte Microsoft Word nebo LibreOffice; vlastní cestu k soffice.exe lze nastavit pomocí ZZZO_LIBREOFFICE.");
      var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = work };
      foreach (string arg in new[] { "-env:UserInstallation=" + new Uri(Path.Combine(work, "profile")).AbsoluteUri,
        "--headless", "--nologo", "--nodefault", "--nofirststartwizard", "--norestore", "--convert-to", "pdf:writer_pdf_Export", "--outdir", work, input }) info.ArgumentList.Add(arg);
      using var process = Process.Start(info) ?? throw new IOException("Převodník LibreOffice se nepodařilo spustit.");
      var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
      var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
      using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      timeout.CancelAfter(TimeSpan.FromMinutes(2));
      try { await process.WaitForExitAsync(timeout.Token); }
      catch (OperationCanceledException)
      {
        try { process.Kill(true); await process.WaitForExitAsync(CancellationToken.None); } catch (InvalidOperationException) { }
        if (cancellationToken.IsCancellationRequested) throw;
        throw new TimeoutException("Převod DOCX do PDF překročil dvě minuty.");
      }
      string diagnostics = (await stdout) + Environment.NewLine + (await stderr);
      if (process.ExitCode != 0 || !File.Exists(output)) throw new IOException("LibreOffice nevytvořil PDF. " + diagnostics.Trim());
  }
}
