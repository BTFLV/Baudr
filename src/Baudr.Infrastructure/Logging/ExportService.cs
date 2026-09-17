using System.Globalization;
using System.Text;
using Baudr.Core.Models;
using Baudr.Core.Parsing;

namespace Baudr.Infrastructure.Logging;

public static class ExportService
{
    public static async Task ExportToFileAsync(string filePath, IReadOnlyList<TerminalLine> lines, LogFormat format, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (format == LogFormat.RawBinary)
        {
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            foreach (var line in lines)
            {
                if (line.RawBytes != null && line.RawBytes.Length > 0)
                {
                    await fs.WriteAsync(line.RawBytes, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var bytes = Encoding.UTF8.GetBytes(line.Text + Environment.NewLine);
                    await fs.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                }
            }
            await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        if (format == LogFormat.Csv)
        {
            await writer.WriteLineAsync("Index,Timestamp,Direction,Text").ConfigureAwait(false);
            foreach (var line in lines)
            {
                var escaped = line.Text.Replace("\"", "\"\"", StringComparison.Ordinal);
                var csvLine = $"{line.Index},{line.Timestamp.ToString("o", CultureInfo.InvariantCulture)},{line.Direction},\"{escaped}\"";
                await writer.WriteLineAsync(csvLine.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }
        else if (format == LogFormat.TimestampedText)
        {
            foreach (var line in lines)
            {
                var dirStr = line.Direction == Direction.Rx ? "RX" : "TX";
                var ts = line.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                await writer.WriteLineAsync($"[{ts}] [{dirStr}] {line.Text}".AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            // Plain text
            foreach (var line in lines)
            {
                await writer.WriteLineAsync(line.Text.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}

