using System.Text;
using Baudr.Core.Models;

namespace Baudr.Core.Text;

public static class LineEndingHelper
{
    public static string GetBytesOrChars(LineEnding ending, string custom = "")
    {
        return ending switch
        {
            LineEnding.None => string.Empty,
            LineEnding.Lf => "\n",
            LineEnding.Cr => "\r",
            LineEnding.CrLf => "\r\n",
            LineEnding.Custom => custom,
            _ => "\r\n"
        };
    }

    public static string ApplyLineEnding(string input, LineEnding ending, string custom = "")
    {
        var suffix = GetBytesOrChars(ending, custom);
        return input + suffix;
    }
}

public static class ControlCharacterFormatter
{
    public static string FormatWithControlGlyphs(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var sb = new StringBuilder(text.Length + 16);
        foreach (char c in text)
        {
            switch (c)
            {
                case '\0': sb.Append('␀'); break; // NUL
                case '\a': sb.Append('␇'); break; // BEL
                case '\b': sb.Append('␈'); break; // BS
                case '\t': sb.Append('␉'); break; // HT
                case '\n': sb.Append("␊\n"); break; // LF (keep actual newline for rendering)
                case '\r': sb.Append('␍'); break; // CR
                case '\x1b': sb.Append('␛'); break; // ESC
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"␢{((int)c):X2}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}
