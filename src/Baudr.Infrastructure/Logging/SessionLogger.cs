using System.Globalization;
using System.Text;
using Baudr.Core.Abstractions;
using Baudr.Core.Models;
using Baudr.Core.Parsing;

namespace Baudr.Infrastructure.Logging;

public class SessionLogger : ISessionLogger
{
    private readonly object _lock = new();
    private FileStream? _fileStream;
    private StreamWriter? _writer;
    private LogFormat _format = LogFormat.PlainText;
    private DateTimeOffset? _startedAt;
    private long _bytesWritten;
    private bool _disposed;

    public bool IsLogging
    {
        get { lock (_lock) return _fileStream != null; }
    }

    public string? ActiveFilePath { get; private set; }
    public long BytesWritten => Interlocked.Read(ref _bytesWritten);
    public TimeSpan ElapsedDuration => _startedAt.HasValue ? DateTimeOffset.UtcNow - _startedAt.Value : TimeSpan.Zero;

    public Task StartAsync(string filePath, LogFormat format, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            StopInternal();

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
            if (format != LogFormat.RawBinary)
            {
                _writer = new StreamWriter(_fileStream, Encoding.UTF8);
            }

            _format = format;
            ActiveFilePath = filePath;
            _startedAt = DateTimeOffset.UtcNow;
            Interlocked.Exchange(ref _bytesWritten, 0);

            if (_format == LogFormat.Csv)
            {
                _writer?.WriteLine("Timestamp,Direction,Data");
                _writer?.Flush();
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        lock (_lock)
        {
            StopInternal();
        }
        return Task.CompletedTask;
    }

    private void StopInternal()
    {
        if (_writer != null)
        {
            try { _writer.Flush(); _writer.Dispose(); } catch { /* Ignore */ }
            _writer = null;
        }
        if (_fileStream != null)
        {
            try { _fileStream.Flush(); _fileStream.Dispose(); } catch { /* Ignore */ }
            _fileStream = null;
        }
        ActiveFilePath = null;
        _startedAt = null;
    }

    public async ValueTask LogRxAsync(ReadOnlyMemory<byte> rawBytes, string? formattedText = null, DateTimeOffset? timestamp = null)
    {
        await WriteEntryAsync(rawBytes, Direction.Rx, formattedText, timestamp).ConfigureAwait(false);
    }

    public async ValueTask LogTxAsync(ReadOnlyMemory<byte> rawBytes, string? formattedText = null, DateTimeOffset? timestamp = null)
    {
        await WriteEntryAsync(rawBytes, Direction.Tx, formattedText, timestamp).ConfigureAwait(false);
    }

    private async ValueTask WriteEntryAsync(ReadOnlyMemory<byte> rawBytes, Direction direction, string? formattedText, DateTimeOffset? timestamp)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow;

        if (_format == LogFormat.RawBinary)
        {
            var stream = _fileStream;
            if (stream == null) return;

            try
            {
                await stream.WriteAsync(rawBytes).ConfigureAwait(false);
                Interlocked.Add(ref _bytesWritten, rawBytes.Length);
            }
            catch
            {
                // Ignore transient write errors
            }
            return;
        }

        var writer = _writer;
        if (writer == null) return;

        var text = formattedText ?? Encoding.UTF8.GetString(rawBytes.Span);
        string line;

        if (_format == LogFormat.TimestampedText)
        {
            var dirStr = direction == Direction.Rx ? "RX" : "TX";
            line = $"[{ts.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}] [{dirStr}] {text}";
        }
        else if (_format == LogFormat.Csv)
        {
            var escaped = text.Replace("\"", "\"\"", StringComparison.Ordinal);
            line = $"{ts.ToString("o", CultureInfo.InvariantCulture)},{direction},\"{escaped}\"";
        }
        else
        {
            line = text;
        }

        try
        {
            await writer.WriteLineAsync(line.AsMemory()).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            Interlocked.Add(ref _bytesWritten, Encoding.UTF8.GetByteCount(line) + 2);
        }
        catch
        {
            // Ignore transient write errors
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await StopAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}

