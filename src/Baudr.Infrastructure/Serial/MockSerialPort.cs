using System.Buffers;
using System.IO.Pipelines;
using Baudr.Core.Abstractions;
using Baudr.Core.Models;

namespace Baudr.Infrastructure.Serial;

public class MockSerialPort : ISerialPort
{
    private readonly Pipe _rxPipe = new();
    private readonly Pipe _txPipe = new();
    private SerialPortConfig _config = new();
    private bool _isOpen;
    private bool _disposed;
    private ModemSignals _signals = new(true, true, true, false);

    public string PortName => _config.PortName;
    public bool IsOpen => _isOpen;
    public SerialPortConfig CurrentConfig => _config;
    public Stream? BaseStream => _rxPipe.Reader.AsStream();

    public bool EchoWrites { get; set; }
    public bool DtrState { get; private set; }
    public bool RtsState { get; private set; }
    public bool BreakState { get; private set; }

    public Task OpenAsync(SerialPortConfig config, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _config = config;
        _isOpen = true;
        return Task.CompletedTask;
    }

    public Task CloseAsync()
    {
        _isOpen = false;
        return Task.CompletedTask;
    }

    private bool _disconnectedAbruptly;

    public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_disconnectedAbruptly) throw new IOException("Simulated device unplugged.");
        if (!_isOpen) throw new InvalidOperationException("Port is closed.");

        var readResult = await _rxPipe.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        var seq = readResult.Buffer;

        if (seq.IsEmpty && readResult.IsCompleted)
        {
            return 0;
        }

        int bytesToCopy = (int)Math.Min(buffer.Length, seq.Length);
        seq.Slice(0, bytesToCopy).CopyTo(buffer.Span);
        _rxPipe.Reader.AdvanceTo(seq.GetPosition(bytesToCopy));

        return bytesToCopy;
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (!_isOpen) throw new InvalidOperationException("Port is closed.");

        await _txPipe.Writer.WriteAsync(data, cancellationToken).ConfigureAwait(false);

        if (EchoWrites)
        {
            await SimulateReceiveAsync(data, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask SimulateReceiveAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        await _rxPipe.Writer.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await _rxPipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void TriggerDisconnect()
    {
        _disconnectedAbruptly = true;
        _isOpen = false;
        _rxPipe.Writer.Complete(new IOException("Simulated device unplugged."));
    }

    public void SetSignals(ModemSignals signals) => _signals = signals;
    public ModemSignals ReadSignals() => _signals;
    public void SetDtr(bool enable) => DtrState = enable;
    public void SetRts(bool enable) => RtsState = enable;
    public void SetBreak(bool enable) => BreakState = enable;

    public ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _isOpen = false;
        _rxPipe.Writer.Complete();
        _rxPipe.Reader.Complete();
        _txPipe.Writer.Complete();
        _txPipe.Reader.Complete();
        GC.SuppressFinalize(this);
    }
}
