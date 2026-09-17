using System.Collections.Concurrent;
using System.Text;
using Baudr.Core.Models;
using Baudr.Core.Parsing;
using Baudr.Core.Text;

namespace Baudr.Core.Buffers;

public class RxBatchProcessor : IAsyncDisposable, IDisposable
{
    private readonly TerminalBuffer _terminalBuffer;
    private readonly SessionStatistics _statistics;
    private readonly ConcurrentQueue<(byte[] Data, Direction Direction, DateTimeOffset Timestamp)> _incomingQueue = new();
    private readonly StringBuilder _partialLineBuffer = new();
    private readonly List<byte> _partialByteBuffer = [];
    private readonly AnsiParser _ansiParser = new();
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processTask;
    private DateTimeOffset _sessionStartTime = DateTimeOffset.UtcNow;
    private bool _disposed;

    public TerminalMode Mode { get; set; } = TerminalMode.Text;
    public bool InterpretAnsi { get; set; } = true;
    public bool ShowControlCharacters { get; set; }
    public string EncodingName { get; set; } = "UTF-8";
    public Action<List<PlotDataPoint>>? OnPlotPointsExtracted { get; set; }
    public Func<ReadOnlyMemory<byte>, ValueTask>? RawDataLogger { get; set; }

    public RxBatchProcessor(TerminalBuffer terminalBuffer, SessionStatistics statistics, int batchIntervalMs = 30)
    {
        _terminalBuffer = terminalBuffer;
        _statistics = statistics;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(10, batchIntervalMs)));
        _processTask = Task.Run(ProcessLoopAsync);
    }

    public void ResetSessionTime()
    {
        _sessionStartTime = DateTimeOffset.UtcNow;
    }

    public void Enqueue(byte[] data, Direction direction)
    {
        if (data.Length == 0) return;

        var now = DateTimeOffset.UtcNow;
        if (direction == Direction.Rx)
        {
            _statistics.AddRx(data.Length);
        }
        else if (direction == Direction.Tx)
        {
            _statistics.AddTx(data.Length);
        }

        _incomingQueue.Enqueue((data, direction, now));
    }

    private async Task ProcessLoopAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
            {
                FlushQueue();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RxBatchProcessor error: {ex}");
        }
    }

    public void FlushQueue()
    {
        if (_incomingQueue.IsEmpty && _partialLineBuffer.Length == 0 && _partialByteBuffer.Count == 0) return;

        var linesToAdd = new List<TerminalLine>();
        var plotPoints = new List<PlotDataPoint>();
        long sampleIndex = _statistics.RxLines;

        var encoding = TextEncodingHelper.GetEncoding(EncodingName);

        while (_incomingQueue.TryDequeue(out var item))
        {
            // Log raw bytes directly
            if (RawDataLogger != null)
            {
                try
                {
                    RawDataLogger(item.Data).AsTask().Wait(50);
                }
                catch
                {
                    // Ignore logger delays
                }
            }

            if (Mode == TerminalMode.Hex)
            {
                ProcessHexMode(item.Data, item.Direction, item.Timestamp, linesToAdd);
            }
            else
            {
                ProcessTextMode(item.Data, item.Direction, item.Timestamp, encoding, linesToAdd, plotPoints, ref sampleIndex);
            }
        }

        if (linesToAdd.Count > 0)
        {
            _terminalBuffer.AddRange(linesToAdd);
        }

        if (plotPoints.Count > 0 && OnPlotPointsExtracted != null)
        {
            OnPlotPointsExtracted(plotPoints);
        }
    }

    private void ProcessTextMode(
        byte[] data,
        Direction direction,
        DateTimeOffset timestamp,
        Encoding encoding,
        List<TerminalLine> linesToAdd,
        List<PlotDataPoint> plotPoints,
        ref long sampleIndex)
    {
        var text = TextEncodingHelper.Decode(data, encoding);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                string rawLine = _partialLineBuffer.ToString().TrimEnd('\r');
                _partialLineBuffer.Clear();

                CreateTerminalLine(rawLine, direction, timestamp, linesToAdd, plotPoints, ref sampleIndex);
            }
            else
            {
                _partialLineBuffer.Append(c);
            }
        }

        // If buffer gets too long without newline, flush partial line
        if (_partialLineBuffer.Length > 2000)
        {
            string rawLine = _partialLineBuffer.ToString();
            _partialLineBuffer.Clear();
            CreateTerminalLine(rawLine, direction, timestamp, linesToAdd, plotPoints, ref sampleIndex);
        }
    }

    private void CreateTerminalLine(
        string rawLine,
        Direction direction,
        DateTimeOffset timestamp,
        List<TerminalLine> linesToAdd,
        List<PlotDataPoint> plotPoints,
        ref long sampleIndex)
    {
        string cleanText = rawLine;
        List<AnsiSpan>? spans = null;

        if (InterpretAnsi)
        {
            var parsed = _ansiParser.Parse(rawLine);
            cleanText = parsed.CleanText;
            if (parsed.Spans.Count > 0)
            {
                spans = parsed.Spans;
            }
        }

        if (ShowControlCharacters)
        {
            cleanText = ControlCharacterFormatter.FormatWithControlGlyphs(cleanText);
        }

        var line = new TerminalLine
        {
            Timestamp = timestamp,
            RelativeTime = timestamp - _sessionStartTime,
            Direction = direction,
            Text = cleanText,
            Spans = spans
        };

        linesToAdd.Add(line);

        // Extract telemetry for plotter if Rx
        if (direction == Direction.Rx)
        {
            var points = PlotDataParser.ParseLine(cleanText, timestamp, sampleIndex++);
            if (points.Count > 0)
            {
                plotPoints.AddRange(points);
            }
        }
    }

    private void ProcessHexMode(
        byte[] data,
        Direction direction,
        DateTimeOffset timestamp,
        List<TerminalLine> linesToAdd)
    {
        _partialByteBuffer.AddRange(data);

        while (_partialByteBuffer.Count >= 16)
        {
            var chunk = _partialByteBuffer.GetRange(0, 16).ToArray();
            _partialByteBuffer.RemoveRange(0, 16);

            var hexDump = HexFormatter.ToHexDump(chunk, 16);
            linesToAdd.Add(new TerminalLine
            {
                Timestamp = timestamp,
                RelativeTime = timestamp - _sessionStartTime,
                Direction = direction,
                Text = hexDump,
                RawBytes = chunk
            });
        }
    }

    public void FlushRemaining()
    {
        if (_partialLineBuffer.Length > 0)
        {
            var now = DateTimeOffset.UtcNow;
            long sampleIndex = _statistics.RxLines;
            var linesToAdd = new List<TerminalLine>();
            var plotPoints = new List<PlotDataPoint>();

            CreateTerminalLine(_partialLineBuffer.ToString(), Direction.Rx, now, linesToAdd, plotPoints, ref sampleIndex);
            _partialLineBuffer.Clear();

            if (linesToAdd.Count > 0) _terminalBuffer.AddRange(linesToAdd);
            if (plotPoints.Count > 0) OnPlotPointsExtracted?.Invoke(plotPoints);
        }

        if (_partialByteBuffer.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var chunk = _partialByteBuffer.ToArray();
            _partialByteBuffer.Clear();

            var hexDump = HexFormatter.ToHexDump(chunk, 16);
            _terminalBuffer.Add(new TerminalLine
            {
                Timestamp = now,
                RelativeTime = now - _sessionStartTime,
                Direction = Direction.Rx,
                Text = hexDump,
                RawBytes = chunk
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        try
        {
            await _processTask.ConfigureAwait(false);
        }
        catch
        {
            // Ignore
        }

        FlushRemaining();
        _timer.Dispose();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
