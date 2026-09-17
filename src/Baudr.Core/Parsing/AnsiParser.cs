using System.Text;
using Baudr.Core.Models;

namespace Baudr.Core.Parsing;

public class AnsiParser
{
    private static readonly ColorRgb[] StandardColors =
    [
        new(0, 0, 0),       // 0: Black
        new(205, 49, 49),   // 1: Red
        new(13, 188, 121),  // 2: Green
        new(229, 229, 16),  // 3: Yellow
        new(36, 114, 200),  // 4: Blue
        new(188, 63, 188),  // 5: Magenta
        new(17, 168, 205),  // 6: Cyan
        new(229, 229, 229), // 7: White
        new(102, 102, 102), // 8: Bright Black
        new(241, 76, 76),   // 9: Bright Red
        new(35, 209, 139),  // 10: Bright Green
        new(245, 245, 67),  // 11: Bright Yellow
        new(59, 142, 234),  // 12: Bright Blue
        new(214, 112, 214), // 13: Bright Magenta
        new(41, 184, 219),  // 14: Bright Cyan
        new(255, 255, 255)  // 15: Bright White
    ];

    private ColorRgb? _currentFg;
    private ColorRgb? _currentBg;
    private bool _bold;
    private bool _dim;
    private bool _italic;
    private bool _underline;
    private bool _invert;

    public void ResetState()
    {
        _currentFg = null;
        _currentBg = null;
        _bold = false;
        _dim = false;
        _italic = false;
        _underline = false;
        _invert = false;
    }

    public (string CleanText, List<AnsiSpan> Spans) Parse(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return (string.Empty, []);
        }

        var sb = new StringBuilder(rawText.Length);
        var spans = new List<AnsiSpan>();
        int currentSpanStart = 0;

        int i = 0;
        int len = rawText.Length;

        while (i < len)
        {
            char c = rawText[i];

            if (c == '\x1b') // ESC
            {
                // Close active span if we have text
                if (sb.Length > currentSpanStart && HasActiveStyle())
                {
                    spans.Add(new AnsiSpan(
                        currentSpanStart,
                        sb.Length - currentSpanStart,
                        _currentFg,
                        _currentBg,
                        _bold,
                        _dim,
                        _italic,
                        _underline,
                        _invert));
                }

                if (i + 1 < len && rawText[i + 1] == '[') // CSI
                {
                    int csiStart = i + 2;
                    int csiEnd = csiStart;
                    while (csiEnd < len && !char.IsLetter(rawText[csiEnd]))
                    {
                        csiEnd++;
                    }

                    if (csiEnd < len)
                    {
                        char command = rawText[csiEnd];
                        var paramStr = rawText[csiStart..csiEnd];

                        if (command == 'm') // SGR: Select Graphic Rendition
                        {
                            ApplySgr(paramStr);
                        }

                        i = csiEnd + 1;
                        currentSpanStart = sb.Length;
                        continue;
                    }
                }

                // If not valid CSI sequence, skip ESC and continue safely
                i++;
                currentSpanStart = sb.Length;
                continue;
            }

            if (c == '\b')
            {
                if (sb.Length > 0)
                {
                    sb.Length--;
                    if (currentSpanStart > sb.Length) currentSpanStart = sb.Length;
                }
                i++;
                continue;
            }

            sb.Append(c);
            i++;
        }

        // Final span
        if (sb.Length > currentSpanStart && HasActiveStyle())
        {
            spans.Add(new AnsiSpan(
                currentSpanStart,
                sb.Length - currentSpanStart,
                _currentFg,
                _currentBg,
                _bold,
                _dim,
                _italic,
                _underline,
                _invert));
        }

        return (sb.ToString(), spans);
    }

    private bool HasActiveStyle() => 
        _currentFg != null || _currentBg != null || _bold || _dim || _italic || _underline || _invert;

    private void ApplySgr(string paramStr)
    {
        if (string.IsNullOrWhiteSpace(paramStr))
        {
            ResetState();
            return;
        }

        var parts = paramStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
        int pIndex = 0;

        while (pIndex < parts.Length)
        {
            if (!int.TryParse(parts[pIndex], out int code))
            {
                pIndex++;
                continue;
            }

            switch (code)
            {
                case 0:
                    ResetState();
                    break;
                case 1:
                    _bold = true;
                    _dim = false;
                    break;
                case 2:
                    _dim = true;
                    _bold = false;
                    break;
                case 3:
                    _italic = true;
                    break;
                case 4:
                    _underline = true;
                    break;
                case 7:
                    _invert = true;
                    break;
                case 22:
                    _bold = false;
                    _dim = false;
                    break;
                case 23:
                    _italic = false;
                    break;
                case 24:
                    _underline = false;
                    break;
                case 27:
                    _invert = false;
                    break;

                // Standard Foreground (30-37)
                case >= 30 and <= 37:
                    _currentFg = StandardColors[code - 30];
                    break;
                case 39:
                    _currentFg = null;
                    break;

                // Standard Background (40-47)
                case >= 40 and <= 47:
                    _currentBg = StandardColors[code - 40];
                    break;
                case 49:
                    _currentBg = null;
                    break;

                // Bright Foreground (90-97)
                case >= 90 and <= 97:
                    _currentFg = StandardColors[code - 90 + 8];
                    break;

                // Bright Background (100-107)
                case >= 100 and <= 107:
                    _currentBg = StandardColors[code - 100 + 8];
                    break;

                // Extended colors: 38 (FG) or 48 (BG)
                case 38 or 48:
                    bool isFg = code == 38;
                    if (pIndex + 1 < parts.Length && int.TryParse(parts[pIndex + 1], out int mode))
                    {
                        if (mode == 5 && pIndex + 2 < parts.Length && int.TryParse(parts[pIndex + 2], out int colorIndex))
                        {
                            // 256 colors
                            var c = Get256Color(colorIndex);
                            if (isFg) _currentFg = c; else _currentBg = c;
                            pIndex += 2;
                        }
                        else if (mode == 2 && pIndex + 4 < parts.Length &&
                                 int.TryParse(parts[pIndex + 2], out int r) &&
                                 int.TryParse(parts[pIndex + 3], out int g) &&
                                 int.TryParse(parts[pIndex + 4], out int b))
                        {
                            // 24-bit RGB
                            var c = new ColorRgb((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));
                            if (isFg) _currentFg = c; else _currentBg = c;
                            pIndex += 4;
                        }
                    }
                    break;
            }

            pIndex++;
        }
    }

    private static ColorRgb Get256Color(int index)
    {
        index = Math.Clamp(index, 0, 255);
        if (index < 16) return StandardColors[index];

        if (index >= 16 && index <= 231)
        {
            // 6x6x6 color cube
            int cube = index - 16;
            int r = (cube / 36) % 6;
            int g = (cube / 6) % 6;
            int b = cube % 6;
            return new ColorRgb(
                (byte)(r == 0 ? 0 : 55 + r * 40),
                (byte)(g == 0 ? 0 : 55 + g * 40),
                (byte)(b == 0 ? 0 : 55 + b * 40));
        }

        // Grayscale 232-255
        int gray = 8 + (index - 232) * 10;
        return new ColorRgb((byte)gray, (byte)gray, (byte)gray);
    }
}

