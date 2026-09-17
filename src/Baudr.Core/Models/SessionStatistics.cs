namespace Baudr.Core.Models;

public class SessionStatistics
{
    private readonly object _sync = new();
    private long _rxBytes;
    private long _txBytes;
    private long _rxLines;
    private long _txLines;
    private DateTimeOffset? _connectedAt;
    private DateTimeOffset _lastCalculationTime = DateTimeOffset.UtcNow;
    private long _lastRxBytes;
    private long _lastTxBytes;

    public long RxBytes => Interlocked.Read(ref _rxBytes);
    public long TxBytes => Interlocked.Read(ref _txBytes);
    public long RxLines => Interlocked.Read(ref _rxLines);
    public long TxLines => Interlocked.Read(ref _txLines);

    public double CurrentRxRate { get; private set; }
    public double CurrentTxRate { get; private set; }
    public double PeakRxRate { get; private set; }
    public double PeakTxRate { get; private set; }

    public TimeSpan ConnectedDuration => _connectedAt.HasValue 
        ? DateTimeOffset.UtcNow - _connectedAt.Value 
        : TimeSpan.Zero;

    public void OnConnected()
    {
        lock (_sync)
        {
            _connectedAt = DateTimeOffset.UtcNow;
            _lastCalculationTime = DateTimeOffset.UtcNow;
            _lastRxBytes = RxBytes;
            _lastTxBytes = TxBytes;
        }
    }

    public void OnDisconnected()
    {
        lock (_sync)
        {
            _connectedAt = null;
            CurrentRxRate = 0;
            CurrentTxRate = 0;
        }
    }

    public void AddRx(long bytes, long lines = 1)
    {
        Interlocked.Add(ref _rxBytes, bytes);
        Interlocked.Add(ref _rxLines, lines);
    }

    public void AddTx(long bytes, long lines = 1)
    {
        Interlocked.Add(ref _txBytes, bytes);
        Interlocked.Add(ref _txLines, lines);
    }

    public void UpdateRates()
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            var elapsed = (now - _lastCalculationTime).TotalSeconds;
            if (elapsed <= 0.2) return;

            long curRx = RxBytes;
            long curTx = TxBytes;

            CurrentRxRate = (curRx - _lastRxBytes) / elapsed;
            CurrentTxRate = (curTx - _lastTxBytes) / elapsed;

            if (CurrentRxRate > PeakRxRate) PeakRxRate = CurrentRxRate;
            if (CurrentTxRate > PeakTxRate) PeakTxRate = CurrentTxRate;

            _lastCalculationTime = now;
            _lastRxBytes = curRx;
            _lastTxBytes = curTx;
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            Interlocked.Exchange(ref _rxBytes, 0);
            Interlocked.Exchange(ref _txBytes, 0);
            Interlocked.Exchange(ref _rxLines, 0);
            Interlocked.Exchange(ref _txLines, 0);
            _lastRxBytes = 0;
            _lastTxBytes = 0;
            CurrentRxRate = 0;
            CurrentTxRate = 0;
            PeakRxRate = 0;
            PeakTxRate = 0;
            if (_connectedAt.HasValue) _connectedAt = DateTimeOffset.UtcNow;
            _lastCalculationTime = DateTimeOffset.UtcNow;
        }
    }
}

