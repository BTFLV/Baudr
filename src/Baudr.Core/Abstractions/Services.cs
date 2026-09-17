using Baudr.Core.Models;

namespace Baudr.Core.Abstractions;

public interface ISerialPortEnumerator
{
    IReadOnlyList<SerialPortInfo> GetAvailablePorts();
}

public interface ISettingsService
{
    AppSettings Current { get; }
    void Load();
    void Save();
    Task LoadAsync();
    Task SaveAsync();
    void Update(Action<AppSettings> updateAction);
    event EventHandler<AppSettings>? SettingsChanged;
}

public interface ISessionLogger : IAsyncDisposable
{
    bool IsLogging { get; }
    string? ActiveFilePath { get; }
    long BytesWritten { get; }
    TimeSpan ElapsedDuration { get; }

    Task StartAsync(string filePath, LogFormat format, CancellationToken cancellationToken = default);
    Task StopAsync();
    ValueTask LogRxAsync(ReadOnlyMemory<byte> rawBytes, string? formattedText = null, DateTimeOffset? timestamp = null);
    ValueTask LogTxAsync(ReadOnlyMemory<byte> rawBytes, string? formattedText = null, DateTimeOffset? timestamp = null);
}

