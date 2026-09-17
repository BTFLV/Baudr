using System.Text;

namespace Baudr.Core.Text;

public static class TextEncodingHelper
{
    private static readonly Encoding Utf8Resilient = Encoding.GetEncoding("UTF-8", EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
    private static readonly Encoding AsciiResilient = Encoding.GetEncoding("ASCII", EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
    private static readonly Encoding Latin1Resilient = Encoding.GetEncoding("ISO-8859-1", EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);

    public static readonly string[] AvailableEncodings = ["UTF-8", "ASCII", "ISO-8859-1 (Latin-1)"];

    public static Encoding GetEncoding(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Utf8Resilient;

        var upper = name.Trim().ToUpperInvariant();
        if (upper.Contains("ASCII")) return AsciiResilient;
        if (upper.Contains("8859") || upper.Contains("LATIN")) return Latin1Resilient;
        return Utf8Resilient;
    }

    public static string Decode(ReadOnlySpan<byte> bytes, Encoding? encoding = null)
    {
        if (bytes.IsEmpty) return string.Empty;
        var enc = encoding ?? Utf8Resilient;
        return enc.GetString(bytes);
    }

    public static byte[] Encode(string text, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var enc = encoding ?? Utf8Resilient;
        return enc.GetBytes(text);
    }
}

