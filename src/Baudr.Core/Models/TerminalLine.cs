namespace Baudr.Core.Models;

public readonly record struct ColorRgb(byte R, byte G, byte B)
{
    public static readonly ColorRgb White = new(255, 255, 255);
    public static readonly ColorRgb Black = new(0, 0, 0);

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
}

public readonly record struct AnsiSpan(
    int Start,
    int Length,
    ColorRgb? Foreground = null,
    ColorRgb? Background = null,
    bool Bold = false,
    bool Dim = false,
    bool Italic = false,
    bool Underline = false,
    bool Invert = false);

public class TerminalLine
{
    public long Index { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan RelativeTime { get; set; }
    public Direction Direction { get; set; }
    public string Text { get; set; } = string.Empty;
    public byte[]? RawBytes { get; set; }
    public IReadOnlyList<AnsiSpan>? Spans { get; set; }

    public override string ToString() => Text;
}

