using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Baudr.Core.Models;

namespace Baudr.Core.Parsing;

public static partial class PlotDataParser
{
    [GeneratedRegex(@"(?:([a-zA-Z0-9_\-]+)\s*[:=]\s*)?([+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?)")]
    private static partial Regex KeyValueRegex();

    public static List<PlotDataPoint> ParseLine(string line, DateTimeOffset timestamp, long sampleIndex)
    {
        var result = new List<PlotDataPoint>();
        if (string.IsNullOrWhiteSpace(line)) return result;

        var trimmed = line.Trim();

        // 1. Try JSON if it starts with { and ends with }
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetDouble(out double val))
                        {
                            result.Add(new PlotDataPoint(prop.Name, val, timestamp, sampleIndex));
                        }
                    }
                    if (result.Count > 0) return result;
                }
            }
            catch
            {
                // Fall back to regex/delimiter parsing
            }
        }

        // 2. Key-value or delimited values using regex
        var matches = KeyValueRegex().Matches(trimmed);
        if (matches.Count > 0)
        {
            int autoIndex = 1;
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var valStr = match.Groups[2].Value;

                if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                {
                    string seriesName = string.IsNullOrWhiteSpace(key) ? $"Series {autoIndex++}" : key;
                    result.Add(new PlotDataPoint(seriesName, val, timestamp, sampleIndex));
                }
            }
        }

        return result;
    }
}

