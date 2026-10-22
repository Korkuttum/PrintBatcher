using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
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
/// Jobs are fired one after another with no wait on spooler state or on the
/// source app closing, so a batch sends as fast as the OS can launch each one.
/// </summary>
public class PrintService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".tif", ".tiff"
    };

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

    private static Task PrintViaShellAsync(string filePath, string printerName, int copies, CancellationToken token)
    {
        for (int i = 0; i < Math.Max(1, copies); i++)
        {
            token.ThrowIfCancellationRequested();

            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                Verb = "printto",
                Arguments = $"\"{printerName}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            // Fire-and-forget: launch the handler and move straight to the next job,
            // with no wait on the spooler or on the app closing.
            Process.Start(psi);
        }

        return Task.CompletedTask;
    }
}
