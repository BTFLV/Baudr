using Baudr.Core.Models;

namespace Baudr.Core.Abstractions;

public interface ISerialPort : IAsyncDisposable, IDisposable
{
    string PortName { get; }
    bool IsOpen { get; }
    SerialPortConfig CurrentConfig { get; }
    Stream? BaseStream { get; }

    Task OpenAsync(SerialPortConfig config, CancellationToken cancellationToken = default);
    Task CloseAsync();

    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    ModemSignals ReadSignals();
    void SetDtr(bool enable);
    void SetRts(bool enable);
    void SetBreak(bool enable);
}

