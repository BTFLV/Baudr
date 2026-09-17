using System.Globalization;

namespace Baudr.Core.Parsing;

public static class HexParser
{
    public static bool TryParse(string? input, out byte[] result, out string? error)
    {
        result = [];
        if (string.IsNullOrWhiteSpace(input))
        {
            error = null;
            return true;
        }

        var cleaned = input.Trim();
        // Remove common prefixes/delimiters: '0x', '\x', ',', ';', '-', ':'
        cleaned = cleaned.Replace("0x", " ", StringComparison.OrdinalIgnoreCase)
                         .Replace("\\x", " ", StringComparison.OrdinalIgnoreCase)
                         .Replace(',', ' ')
                         .Replace(';', ' ')
                         .Replace('-', ' ')
                         .Replace(':', ' ');

        var tokens = cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        // Case 1: Tokens of 1-2 hex chars each (e.g. "01 FF A0")
        bool allByteTokens = true;
        foreach (var token in tokens)
        {
            if (token.Length > 2)
            {
                allByteTokens = false;
                break;
            }
        }

        if (allByteTokens && tokens.Length > 0)
        {
            var bytes = new byte[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!byte.TryParse(tokens[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
                {
                    error = $"Invalid hex byte '{tokens[i]}' at position {i + 1}.";
                    return false;
                }
            }
            result = bytes;
            error = null;
            return true;
        }

        // Case 2: Continuous hex string (e.g. "01FFA00D0A")
        var continuous = string.Concat(tokens);
        if (continuous.Length % 2 != 0)
        {
            error = $"Hex string has odd length ({continuous.Length}). Incomplete byte at end.";
            return false;
        }

        var continuousBytes = new byte[continuous.Length / 2];
        for (int i = 0; i < continuousBytes.Length; i++)
        {
            var slice = continuous.AsSpan(i * 2, 2);
            if (!byte.TryParse(slice, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out continuousBytes[i]))
            {
                error = $"Invalid hex pair '{slice.ToString()}' at byte index {i}.";
                return false;
            }
        }

        result = continuousBytes;
        error = null;
        return true;
    }

    public static byte[] Parse(string input)
    {
        if (TryParse(input, out var bytes, out var error))
        {
            return bytes;
        }
        throw new FormatException(error);
    }
}

