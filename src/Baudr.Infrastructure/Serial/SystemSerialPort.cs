using System.IO.Ports;
using Baudr.Core.Abstractions;
using Baudr.Core.Models;

namespace Baudr.Infrastructure.Serial;

public class SystemSerialPort : ISerialPort
{
    private SerialPort? _serialPort;
    private SerialPortConfig _config = new();
    private bool _disposed;

    public string PortName => _config.PortName;
    public bool IsOpen => _serialPort?.IsOpen == true;
    public SerialPortConfig CurrentConfig => _config;
    public Stream? BaseStream => _serialPort?.IsOpen == true ? _serialPort.BaseStream : null;

    public Task OpenAsync(SerialPortConfig config, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!config.IsValid(out var error))
        {
            throw new ArgumentException(error ?? "Invalid serial configuration.", nameof(config));
        }

        CloseInternal();

        _config = config;

        var port = new SerialPort(
            config.PortName,
            config.BaudRate,
            (Parity)config.Parity,
            config.DataBits,
            (StopBits)config.StopBits)
        {
            Handshake = (Handshake)config.Handshake,
            DtrEnable = config.DtrEnable,
            RtsEnable = config.RtsEnable,
            ReadTimeout = config.ReadTimeoutMs > 0 ? config.ReadTimeoutMs : SerialPort.InfiniteTimeout,
            WriteTimeout = config.WriteTimeoutMs > 0 ? config.WriteTimeoutMs : SerialPort.InfiniteTimeout
        };

        try
        {
            port.Open();
            // Discard any residual garbage in hardware buffers
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
            _serialPort = port;
        }
        catch
        {
            try { port.Dispose(); } catch { /* Ignore */ }
            throw;
        }

        return Task.CompletedTask;
    }

    public Task CloseAsync()
    {
        CloseInternal();
        return Task.CompletedTask;
    }

    private void CloseInternal()
    {
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch
            {
                // Ignore errors during port shutdown
            }
            finally
            {
                try { _serialPort.Dispose(); } catch { /* Ignore */ }
                _serialPort = null;
            }
        }
    }

    public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var port = _serialPort;
        if (port == null || !port.IsOpen)
        {
            throw new InvalidOperationException("Serial port is not open.");
        }

        try
        {
            return await port.BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Device was abruptly removed or disconnected
            CloseInternal();
            throw;
        }
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var port = _serialPort;
        if (port == null || !port.IsOpen)
        {
            throw new InvalidOperationException("Serial port is not open.");
        }

        try
        {
            await port.BaseStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            await port.BaseStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (IOException)
        {
            CloseInternal();
            throw;
        }
    }

    public ModemSignals ReadSignals()
    {
        var port = _serialPort;
        if (port == null || !port.IsOpen)
        {
            return ModemSignals.None;
        }

        try
        {
            return new ModemSignals(
                port.CtsHolding,
                port.DsrHolding,
                port.CDHolding,
                false // RI is not exposed in standard System.IO.Ports across all platforms
            );
        }
        catch
        {
            return ModemSignals.None;
        }
    }

    public void SetDtr(bool enable)
    {
        if (_serialPort?.IsOpen == true)
        {
            try { _serialPort.DtrEnable = enable; } catch { /* Unsupported on some adapters */ }
        }
    }

    public void SetRts(bool enable)
    {
        if (_serialPort?.IsOpen == true)
        {
            try { _serialPort.RtsEnable = enable; } catch { /* Unsupported on some adapters */ }
        }
    }

    public void SetBreak(bool enable)
    {
        if (_serialPort?.IsOpen == true)
        {
            try { _serialPort.BreakState = enable; } catch { /* Unsupported on some adapters */ }
        }
    }

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
        CloseInternal();
        GC.SuppressFinalize(this);
    }
}
