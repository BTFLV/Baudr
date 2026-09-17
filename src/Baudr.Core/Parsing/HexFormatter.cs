using System.Globalization;
using System.Text;

namespace Baudr.Core.Parsing;

public static class HexFormatter
{
    public static string ToHexString(ReadOnlySpan<byte> bytes, string delimiter = " ")
    {
        if (bytes.IsEmpty) return string.Empty;

        var sb = new StringBuilder(bytes.Length * (2 + delimiter.Length));
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0 && !string.IsNullOrEmpty(delimiter)) sb.Append(delimiter);
            sb.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    public static string ToHexDump(ReadOnlySpan<byte> bytes, int bytesPerLine = 16, long baseOffset = 0)
    {
        if (bytes.IsEmpty) return string.Empty;

        var sb = new StringBuilder();
        int total = bytes.Length;

        for (int offset = 0; offset < total; offset += bytesPerLine)
        {
            int count = Math.Min(bytesPerLine, total - offset);
            long lineAddr = baseOffset + offset;

            // Offset
            sb.Append(lineAddr.ToString("X8", CultureInfo.InvariantCulture));
            sb.Append("  ");

            // Hex pairs
            for (int i = 0; i < bytesPerLine; i++)
            {
                if (i == 8) sb.Append(' ');

                if (i < count)
                {
                    sb.Append(bytes[offset + i].ToString("X2", CultureInfo.InvariantCulture));
                    sb.Append(' ');
                }
                else
                {
                    sb.Append("   ");
                }
            }

            sb.Append(" |");

            // ASCII gutter
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                char c = (b >= 32 && b <= 126) ? (char)b : '.';
                sb.Append(c);
            }

            sb.Append('|');
            if (offset + bytesPerLine < total)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
