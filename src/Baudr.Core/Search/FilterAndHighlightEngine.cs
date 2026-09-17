using System.Text.RegularExpressions;
using Baudr.Core.Models;

namespace Baudr.Core.Search;

public static class FilterEngine
{
    public static bool Matches(TerminalLine line, DisplayFilter filter)
    {
        if (!filter.IsActive) return true;

        if (filter.RxOnly && line.Direction != Direction.Rx) return false;
        if (filter.TxOnly && line.Direction != Direction.Tx) return false;

        var text = line.Text;

        // Include filter
        if (!string.IsNullOrEmpty(filter.IncludeText))
        {
            if (filter.IsRegex)
            {
                try
                {
                    var opt = filter.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    if (!Regex.IsMatch(text, filter.IncludeText, opt, TimeSpan.FromMilliseconds(100)))
                    {
                        return false;
                    }
                }
                catch
                {
                    // If regex invalid, treat as non-match or ignore
                    return false;
                }
            }
            else
            {
                var comp = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (!text.Contains(filter.IncludeText, comp))
                {
                    return false;
                }
            }
        }

        // Exclude filter
        if (!string.IsNullOrEmpty(filter.ExcludeText))
        {
            if (filter.IsRegex)
            {
                try
                {
                    var opt = filter.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    if (Regex.IsMatch(text, filter.ExcludeText, opt, TimeSpan.FromMilliseconds(100)))
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                var comp = filter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (text.Contains(filter.ExcludeText, comp))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public static class HighlightEngine
{
    public static HighlightRule? GetMatchingRule(TerminalLine line, IReadOnlyList<HighlightRule> rules)
    {
        if (rules.Count == 0 || string.IsNullOrEmpty(line.Text)) return null;

        foreach (var rule in rules)
        {
            if (!rule.IsEnabled || string.IsNullOrEmpty(rule.Pattern)) continue;

            if (rule.IsRegex)
            {
                try
                {
                    var opt = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    if (Regex.IsMatch(line.Text, rule.Pattern, opt, TimeSpan.FromMilliseconds(100)))
                    {
                        return rule;
                    }
                }
                catch
                {
                    // Skip invalid regex
                }
            }
            else
            {
                var comp = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (line.Text.Contains(rule.Pattern, comp))
                {
                    return rule;
                }
            }
        }

        return null;
    }
}

