using Baudr.Core.Models;
using Baudr.Core.Search;
using Xunit;

namespace Baudr.Core.Tests;

public class SearchAndFilterTests
{
    private readonly List<TerminalLine> _sampleLines =
    [
        new() { Index = 1, Text = "Booting Baudr core v1.0", Direction = Direction.System },
        new() { Index = 2, Text = "ERROR: Failed to initialize sensor", Direction = Direction.Rx },
        new() { Index = 3, Text = "WARN: Retrying sensor setup", Direction = Direction.Rx },
        new() { Index = 4, Text = "Sensor OK. Temperature = 24.5C", Direction = Direction.Rx },
        new() { Index = 5, Text = "TX: STATUS_REQ", Direction = Direction.Tx }
    ];

    [Fact]
    public void SearchEngine_PlainTextCaseInsensitive_FindsMatches()
    {
        var query = new SearchQuery { Pattern = "sensor", CaseSensitive = false };
        var result = SearchEngine.Execute(_sampleLines, query);

        Assert.Equal(3, result.TotalMatches);
        Assert.Equal(1, result.MatchingLineIndices[0]); // line 2 (index 1)
        Assert.Equal(2, result.MatchingLineIndices[1]); // line 3 (index 2)
        Assert.Equal(3, result.MatchingLineIndices[2]); // line 4 (index 3)
    }

    [Fact]
    public void SearchEngine_RegexSearch_FindsMatches()
    {
        var query = new SearchQuery { Pattern = @"ERROR|WARN", IsRegex = true };
        var result = SearchEngine.Execute(_sampleLines, query);

        Assert.Equal(2, result.TotalMatches);
        Assert.Equal(1, result.MatchingLineIndices[0]);
        Assert.Equal(2, result.MatchingLineIndices[1]);
    }

    [Fact]
    public void SearchEngine_InvalidRegex_ReturnsFriendlyErrorWithoutThrowing()
    {
        var query = new SearchQuery { Pattern = "[invalid(", IsRegex = true };
        var result = SearchEngine.Execute(_sampleLines, query);

        Assert.NotNull(result.ErrorMessage);
        Assert.Empty(result.MatchingLineIndices);
    }

    [Fact]
    public void SearchEngine_Navigation_CyclesCorrectly()
    {
        var query = new SearchQuery { Pattern = "sensor" };
        var result = SearchEngine.Execute(_sampleLines, query);

        Assert.Equal(0, result.CurrentMatchIndex);
        result.MoveNext();
        Assert.Equal(1, result.CurrentMatchIndex);
        result.MoveNext();
        Assert.Equal(2, result.CurrentMatchIndex);
        result.MoveNext();
        Assert.Equal(0, result.CurrentMatchIndex); // wrapped
        result.MovePrevious();
        Assert.Equal(2, result.CurrentMatchIndex); // wrapped back
    }

    [Fact]
    public void FilterEngine_IncludeAndExclude_FiltersCorrectly()
    {
        var filter = new DisplayFilter { IncludeText = "sensor", ExcludeText = "ERROR" };

        Assert.False(FilterEngine.Matches(_sampleLines[0], filter)); // No "sensor"
        Assert.False(FilterEngine.Matches(_sampleLines[1], filter)); // Has "ERROR"
        Assert.True(FilterEngine.Matches(_sampleLines[2], filter));  // Has "sensor", no "ERROR"
        Assert.True(FilterEngine.Matches(_sampleLines[3], filter));  // Has "sensor", no "ERROR"
    }

    [Fact]
    public void FilterEngine_DirectionFilter_FiltersRxOnly()
    {
        var filter = new DisplayFilter { RxOnly = true };

        Assert.False(FilterEngine.Matches(_sampleLines[0], filter)); // System
        Assert.True(FilterEngine.Matches(_sampleLines[1], filter));  // Rx
        Assert.False(FilterEngine.Matches(_sampleLines[4], filter)); // Tx
    }

    [Fact]
    public void HighlightEngine_MatchesExpectedRule()
    {
        var rules = HighlightRule.CreateDefaultRules();
        var errorLine = _sampleLines[1]; // "ERROR: Failed..."

        var matchedRule = HighlightEngine.GetMatchingRule(errorLine, rules);
        Assert.NotNull(matchedRule);
        Assert.Equal("Error", matchedRule.Name);
    }
}

