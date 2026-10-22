using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Printing;
using PrintBatcher.Models;
using DuplexMode = PrintBatcher.Models.DuplexMode;
using ScaleMode = PrintBatcher.Models.ScaleMode;
using PageOrientation = PrintBatcher.Models.PageOrientation;

namespace PrintBatcher.Services;

/// <summary>
/// Prints arbitrary file types. Raster images are rendered and sent to the
/// spooler directly, honoring the app's color/orientation/duplex/scale
/// settings; every other registered file type (PDF, Office docs, text, etc.)
/// is handed to its own default application via the Windows shell "printto"
/// verb, the same mechanism Explorer's right-click "Print" uses, and follows
/// that printer's current driver defaults since no universal API exists to
/// inject print options into an arbitrary third-party app's print flow.
/// </summary>
public class PrintService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".tif", ".tiff"
    };

    private static readonly TimeSpan SpoolDetectionTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan SpoolPollInterval = TimeSpan.FromMilliseconds(150);

    public static IReadOnlyList<string> GetInstalledPrinters()
    {
        var printers = new List<string>();
        foreach (string name in PrinterSettings.InstalledPrinters)
            printers.Add(name);
        return printers;
    }

    public static string? GetDefaultPrinter()
    {
        var settings = new PrinterSettings();
        return settings.IsDefaultPrinter ? settings.PrinterName : null;
    }

    /// <summary>Opens the OS-native printer preferences dialog for the given printer (rundll32 printui.dll).</summary>
    public static void OpenPrinterProperties(string printerName)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "rundll32.exe",
            Arguments = $"printui.dll,PrintUIEntry /p /n \"{printerName}\"",
            UseShellExecute = true,
        });
    }

    public async Task PrintAsync(string filePath, string printerName, int copies, AppSettings settings, CancellationToken token)
    {
        string ext = Path.GetExtension(filePath);

        if (ImageExtensions.Contains(ext))
        {
            await Task.Run(() => PrintImage(filePath, printerName, copies, settings), token);
        }
        else
        {
            await PrintViaShellAsync(filePath, printerName, copies, token);
        }
    }

    private static void PrintImage(string filePath, string printerName, int copies, AppSettings settings)
    {
        using var image = Image.FromFile(filePath);
        using var doc = new PrintDocument();
        doc.PrinterSettings.PrinterName = printerName;
        doc.PrinterSettings.Copies = (short)Math.Clamp(copies, 1, short.MaxValue);
        doc.PrinterSettings.Collate = settings.Collate;

        if (!doc.PrinterSettings.IsValid)
            throw new InvalidOperationException($"'{printerName}' geçerli bir yazıcı değil.");

        doc.PrinterSettings.Duplex = settings.Duplex switch
        {
            DuplexMode.LongEdge => System.Drawing.Printing.Duplex.Vertical,
            DuplexMode.ShortEdge => System.Drawing.Printing.Duplex.Horizontal,
            _ => System.Drawing.Printing.Duplex.Simplex,
        };

        bool landscape = settings.Orientation switch
        {
            PageOrientation.Landscape => true,
            PageOrientation.Portrait => false,
            _ => image.Width > image.Height,
        };
        doc.DefaultPageSettings.Landscape = landscape;

        if (settings.ColorMode != ColorMode.Auto)
            doc.DefaultPageSettings.Color = settings.ColorMode == ColorMode.Color;

        doc.PrintPage += (_, e) =>
        {
            if (e.Graphics is null || e.MarginBounds.IsEmpty) return;

            var bounds = e.MarginBounds;
            double scale = settings.ScaleMode switch
            {
                ScaleMode.ActualSize => 1.0,
                ScaleMode.Custom => settings.CustomScalePercent / 100.0,
                _ => Math.Min((double)bounds.Width / image.Width, (double)bounds.Height / image.Height),
            };

            int width = (int)(image.Width * scale);
            int height = (int)(image.Height * scale);
            int x = bounds.X + (bounds.Width - width) / 2;
            int y = bounds.Y + (bounds.Height - height) / 2;

            e.Graphics.DrawImage(image, x, y, width, height);
        };

        doc.Print();
    }

    private static async Task PrintViaShellAsync(string filePath, string printerName, int copies, CancellationToken token)
    {
        using var server = new PrintServer();
        using var queue = server.GetPrintQueue(printerName);

        for (int i = 0; i < Math.Max(1, copies); i++)
        {
            token.ThrowIfCancellationRequested();

            int baselineJobId = GetMaxJobId(queue);

            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                Verb = "printto",
                Arguments = $"\"{printerName}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            // Fire-and-forget: the viewer app only needs to live long enough to hand the
            // job to the spooler. We don't wait for it to fully close (some apps, e.g.
            // Word, stay open), which is what made batches crawl one file at a time.
            Process.Start(psi);

            await WaitForJobToReachSpoolerAsync(queue, baselineJobId, token);
        }
    }

    private static int GetMaxJobId(PrintQueue queue)
    {
        try
        {
            queue.Refresh();
            int max = 0;
            using var jobs = queue.GetPrintJobInfoCollection();
            foreach (PrintSystemJobInfo job in jobs)
                max = Math.Max(max, job.JobIdentifier);
            return max;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Polls the printer's queue until a job newer than <paramref name="baselineJobId"/>
    /// shows up, then returns immediately so the next file can be sent — instead of
    /// waiting for the source application to finish printing or close.
    /// </summary>
    private static async Task WaitForJobToReachSpoolerAsync(PrintQueue queue, int baselineJobId, CancellationToken token)
    {
        var deadline = DateTime.UtcNow + SpoolDetectionTimeout;

        while (DateTime.UtcNow < deadline)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                queue.Refresh();
                using var jobs = queue.GetPrintJobInfoCollection();
                foreach (PrintSystemJobInfo job in jobs)
                {
                    if (job.JobIdentifier > baselineJobId)
                        return;
                }
            }
            catch
            {
                // Transient spooler/RPC hiccup: keep polling until the deadline.
            }

            await Task.Delay(SpoolPollInterval, token);
        }

        // Fallback: the app may not use the spooler the way we expect (rare), or it was
        // simply too slow to launch. Move on rather than stalling the whole batch.
    }
}
