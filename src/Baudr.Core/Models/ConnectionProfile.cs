namespace Baudr.Core.Models;

public record ConnectionProfile
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "Default Profile";
    public SerialPortConfig Config { get; init; } = new();
    public LineEnding DefaultLineEnding { get; init; } = LineEnding.CrLf;
    public string CustomLineEnding { get; init; } = string.Empty;
    public bool AutoConnectOnStartup { get; init; }
    public bool AutoReconnectOnDrop { get; init; } = true;
    public int ReconnectDelayMs { get; init; } = 1000;
    public bool LocalEcho { get; init; }
    public bool InterpretAnsi { get; init; } = true;
}
