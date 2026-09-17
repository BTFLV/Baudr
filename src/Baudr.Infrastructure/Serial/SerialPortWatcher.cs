using Baudr.Core.Abstractions;
using Baudr.Core.Models;

namespace Baudr.Infrastructure.Serial;

public class SerialPortWatcher : IAsyncDisposable, IDisposable
{
    private readonly ISerialPortEnumerator _enumerator;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _watchTask;
    private readonly HashSet<string> _knownPorts = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public event Action<IReadOnlyList<SerialPortInfo>>? PortsChanged;

    public SerialPortWatcher(ISerialPortEnumerator enumerator, int checkIntervalMs = 1500)
    {
        _enumerator = enumerator;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(checkIntervalMs));

        // Initial scan
        var initial = _enumerator.GetAvailablePorts();
        foreach (var p in initial)
        {
            _knownPorts.Add(p.PortName);
        }

        _watchTask = Task.Run(WatchLoopAsync);
    }

    private async Task WatchLoopAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
            {
                CheckPorts();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    public void CheckPorts()
    {
        var current = _enumerator.GetAvailablePorts();
        var currentNames = current.Select(p => p.PortName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool changed = !_knownPorts.SetEquals(currentNames);
        if (changed)
        {
            _knownPorts.Clear();
            foreach (var name in currentNames)
            {
                _knownPorts.Add(name);
            }

            PortsChanged?.Invoke(current);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        try
        {
            await _watchTask.ConfigureAwait(false);
        }
        catch
        {
            // Ignore
        }

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

