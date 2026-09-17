namespace Baudr.Core.Models;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Error
}

public enum Direction
{
    Rx,
    Tx,
    System
}

public enum TerminalMode
{
    Text,
    Hex,
    TextAndHex,
    Packet
}

public enum SendMode
{
    Text,
    Hex
}

public enum LineEnding
{
    None,
    Lf,
    Cr,
    CrLf,
    Custom
}

public enum TimestampMode
{
    None,
    Relative,
    AbsoluteTime,
    AbsoluteDateTime
}

public enum SerialParity
{
    None = 0,
    Odd = 1,
    Even = 2,
    Mark = 3,
    Space = 4
}

public enum SerialStopBits
{
    One = 1,
    Two = 2,
    OnePointFive = 3
}

public enum SerialHandshake
{
    None = 0,
    XOnXOff = 1,
    RequestToSend = 2,
    RequestToSendXOnXOff = 3
}

public enum AppTheme
{
    Dark,
    Light,
    System
}

public enum LogFormat
{
    PlainText,
    TimestampedText,
    RawBinary,
    Csv
}

