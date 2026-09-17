using System.Text.RegularExpressions;
using Baudr.Core.Models;

namespace Baudr.Core.Search;

public record SearchQuery
{
    public string Pattern { get; init; } = string.Empty;
    public bool CaseSensitive { get; init; }
    public bool IsRegex { get; init; }
}

public class SearchResult
{
    public List<int> MatchingLineIndices { get; } = [];
    public int CurrentMatchIndex { get; set; } = -1;
    public string? ErrorMessage { get; set; }

    public int TotalMatches => MatchingLineIndices.Count;
    public int CurrentLineIndex => (CurrentMatchIndex >= 0 && CurrentMatchIndex < MatchingLineIndices.Count) 
        ? MatchingLineIndices[CurrentMatchIndex] 
        : -1;

    public void MoveNext()
    {
        if (MatchingLineIndices.Count == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex + 1) % MatchingLineIndices.Count;
    }

    public void MovePrevious()
    {
        if (MatchingLineIndices.Count == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex - 1 + MatchingLineIndices.Count) % MatchingLineIndices.Count;
    }
}

public static class SearchEngine
{
    public static SearchResult Execute(IReadOnlyList<TerminalLine> lines, SearchQuery query)
    {
        var result = new SearchResult();
        if (string.IsNullOrEmpty(query.Pattern)) return result;

        if (query.IsRegex)
        {
            try
            {
                var options = RegexOptions.Compiled;
                if (!query.CaseSensitive) options |= RegexOptions.IgnoreCase;
                var regex = new Regex(query.Pattern, options, TimeSpan.FromMilliseconds(500));

                for (int i = 0; i < lines.Count; i++)
                {
                    if (regex.IsMatch(lines[i].Text))
                    {
                        result.MatchingLineIndices.Add(i);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                result.ErrorMessage = $"Invalid regex: {ex.Message}";
                return result;
            }
            catch (RegexMatchTimeoutException)
            {
                result.ErrorMessage = "Regex search timed out.";
                return result;
            }
        }
        else
        {
            var comparison = query.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Text.Contains(query.Pattern, comparison))
                {
                    result.MatchingLineIndices.Add(i);
                }
            }
        }

        if (result.MatchingLineIndices.Count > 0)
        {
            result.CurrentMatchIndex = 0;
        }

        return result;
    }
}

