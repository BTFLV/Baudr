using System.Collections.ObjectModel;
using System.Text;
using Baudr.Core.Models;

namespace Baudr.Core.Buffers;

public class TerminalBuffer
{
    private readonly object _lock = new();
    private readonly List<TerminalLine> _lines;
    private long _totalLineCounter;

    public int Capacity { get; set; }
    public int Count
    {
        get { lock (_lock) return _lines.Count; }
    }

    public event Action<IReadOnlyList<TerminalLine>>? LinesAdded;
    public event Action? Cleared;

    public TerminalBuffer(int capacity = 10000)
    {
        Capacity = Math.Max(1, capacity);
        _lines = new List<TerminalLine>(Math.Min(Capacity, 2048));
    }

    public void Add(TerminalLine line)
    {
        List<TerminalLine> added;
        lock (_lock)
        {
            line.Index = ++_totalLineCounter;
            _lines.Add(line);
            EnsureCapacityInternal();
            added = [line];
        }

        LinesAdded?.Invoke(added);
    }

    public void AddRange(IReadOnlyList<TerminalLine> lines)
    {
        if (lines.Count == 0) return;

        List<TerminalLine> added;
        lock (_lock)
        {
            added = new List<TerminalLine>(lines.Count);
            foreach (var line in lines)
            {
                line.Index = ++_totalLineCounter;
                _lines.Add(line);
                added.Add(line);
            }
            EnsureCapacityInternal();
        }

        LinesAdded?.Invoke(added);
    }

    private void EnsureCapacityInternal()
    {
        if (_lines.Count > Capacity)
        {
            int excess = _lines.Count - Capacity;
            _lines.RemoveRange(0, excess);
        }
    }

    public IReadOnlyList<TerminalLine> GetSnapshot()
    {
        lock (_lock)
        {
            return _lines.ToArray();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _lines.Clear();
        }
        Cleared?.Invoke();
    }

    public string ExportText(bool includeTimestamps = false, bool includeRxTx = false)
    {
        IReadOnlyList<TerminalLine> snapshot = GetSnapshot();
        var sb = new StringBuilder(snapshot.Count * 64);

        foreach (var line in snapshot)
        {
            if (includeTimestamps)
            {
                sb.Append('[').Append(line.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)).Append("] ");
            }
            if (includeRxTx)
            {
                sb.Append(line.Direction switch
                {
                    Direction.Rx => "RX: ",
                    Direction.Tx => "TX: ",
                    _ => "SYS: "
                });
            }
            sb.AppendLine(line.Text);
        }

        return sb.ToString();
    }
}
