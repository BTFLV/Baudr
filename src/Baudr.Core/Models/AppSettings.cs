namespace Baudr.Core.Models;

public class GeneralSettings
{
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public bool RestoreSessionsOnStartup { get; set; }
    public bool ConfirmOnCloseActiveSession { get; set; } = true;
}

public class TerminalSettings
{
    public string FontFamily { get; set; } = "Cascadia Code, Consolas, Menlo, Monaco, DejaVu Sans Mono, monospace";
    public double FontSize { get; set; } = 13.0;
    public bool LineWrapping { get; set; } = true;
    public TimestampMode TimestampMode { get; set; } = TimestampMode.None;
    public string DefaultEncoding { get; set; } = "UTF-8";
    public int BufferCapacity { get; set; } = 10000;
    public bool InterpretAnsi { get; set; } = true;
    public bool ShowControlCharacters { get; set; }
    public bool ShowLineNumbers { get; set; }
}

public class SerialSettings
{
    public int DefaultBaudRate { get; set; } = 115200;
    public int DefaultDataBits { get; set; } = 8;
    public SerialParity DefaultParity { get; set; } = SerialParity.None;
    public SerialStopBits DefaultStopBits { get; set; } = SerialStopBits.One;
    public LineEnding DefaultLineEnding { get; set; } = LineEnding.CrLf;
    public string CustomLineEnding { get; set; } = string.Empty;
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectDelayMs { get; set; } = 1000;
    public bool DefaultDtr { get; set; } = true;
    public bool DefaultRts { get; set; } = true;
}

public class LoggingSettings
{
    public string DefaultDirectory { get; set; } = string.Empty;
    public string FilenamePattern { get; set; } = "Baudr_{0:yyyy-MM-dd_HH-mm-ss}_{1}";
    public LogFormat DefaultFormat { get; set; } = LogFormat.PlainText;
    public bool AutoFlush { get; set; } = true;
}

public class AppearanceSettings
{
    public string AccentColorHex { get; set; } = "#00B4D8";
    public bool CompactMode { get; set; }
}

public class WindowSettings
{
    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;
    public double? X { get; set; }
    public double? Y { get; set; }
    public bool IsMaximized { get; set; }
}

public class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public GeneralSettings General { get; set; } = new();
    public TerminalSettings Terminal { get; set; } = new();
    public SerialSettings Serial { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public WindowSettings Window { get; set; } = new();

    public List<ConnectionProfile> Profiles { get; set; } = CreateDefaultProfiles();
    public List<SavedCommand> SavedCommands { get; set; } = CreateDefaultSavedCommands();
    public List<HighlightRule> HighlightRules { get; set; } = HighlightRule.CreateDefaultRules();

    public static List<ConnectionProfile> CreateDefaultProfiles() =>
    [
        new() { Name = "Arduino Uno / Nano", Config = new SerialPortConfig { BaudRate = 115200 }, DefaultLineEnding = LineEnding.CrLf },
        new() { Name = "ESP32 / ESP8266", Config = new SerialPortConfig { BaudRate = 115200 }, DefaultLineEnding = LineEnding.CrLf },
        new() { Name = "Classic Modem (9600 8N1)", Config = new SerialPortConfig { BaudRate = 9600 }, DefaultLineEnding = LineEnding.CrLf },
        new() { Name = "High-Speed UART (921600)", Config = new SerialPortConfig { BaudRate = 921600 }, DefaultLineEnding = LineEnding.Lf },
        new() { Name = "Modbus RTU (19200 8E1)", Config = new SerialPortConfig { BaudRate = 19200, Parity = SerialParity.Even, StopBits = SerialStopBits.One }, DefaultLineEnding = LineEnding.None }
    ];

    public static List<SavedCommand> CreateDefaultSavedCommands() =>
    [
        new() { Name = "AT Ping", Payload = "AT", Mode = SendMode.Text, LineEnding = LineEnding.CrLf, Group = "Modem", Description = "Test AT communication" },
        new() { Name = "Help", Payload = "help", Mode = SendMode.Text, LineEnding = LineEnding.CrLf, Group = "CLI", Description = "Request help menu" },
        new() { Name = "Reset / Reboot", Payload = "reboot", Mode = SendMode.Text, LineEnding = LineEnding.CrLf, Group = "System", Description = "Reboot target device" },
        new() { Name = "Ping Bytes (Hex)", Payload = "02 00 01 A0 03", Mode = SendMode.Hex, LineEnding = LineEnding.None, Group = "Binary", Description = "Framed ping packet" }
    ];
}
