namespace Baudr.Core.Models;

public record SerialPortConfig
{
    public string PortName { get; init; } = string.Empty;
    public int BaudRate { get; init; } = 115200;
    public int DataBits { get; init; } = 8;
    public SerialParity Parity { get; init; } = SerialParity.None;
    public SerialStopBits StopBits { get; init; } = SerialStopBits.One;
    public SerialHandshake Handshake { get; init; } = SerialHandshake.None;
    public bool DtrEnable { get; init; } = true;
    public bool RtsEnable { get; init; } = true;
    public int ReadTimeoutMs { get; init; } = 500;
    public int WriteTimeoutMs { get; init; } = 500;
    public string EncodingName { get; init; } = "UTF-8";

    public static readonly int[] StandardBaudRates =
    [
        300, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200,
        230400, 460800, 500000, 576000, 921600, 1000000, 1500000, 2000000
    ];

    public static readonly int[] StandardDataBits = [5, 6, 7, 8];

    public string Shorthand => $"{BaudRate} {DataBits}{Parity.ToString()[0]}{(StopBits == SerialStopBits.OnePointFive ? "1.5" : ((int)StopBits).ToString(System.Globalization.CultureInfo.InvariantCulture))}";

    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(PortName))
        {
            error = "Port name cannot be empty.";
            return false;
        }

        if (BaudRate <= 0)
        {
            error = "Baud rate must be greater than 0.";
            return false;
        }

        if (DataBits < 5 || DataBits > 8)
        {
            error = "Data bits must be between 5 and 8.";
            return false;
        }

        error = null;
        return true;
    }
}
