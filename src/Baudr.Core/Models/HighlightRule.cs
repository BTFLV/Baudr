namespace Baudr.Core.Models;

public record HighlightRule
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public bool IsRegex { get; init; }
    public bool CaseSensitive { get; init; }
    public string? ForegroundHex { get; init; }
    public string? BackgroundHex { get; init; }
    public bool Bold { get; init; }
    public bool IsEnabled { get; init; } = true;

    public static List<HighlightRule> CreateDefaultRules() =>
    [
        new() { Name = "Error", Pattern = "error|fail|fault|fatal|exception", IsRegex = true, ForegroundHex = "#FF5555", Bold = true },
        new() { Name = "Warning", Pattern = "warn|warning", IsRegex = true, ForegroundHex = "#FFB86C", Bold = false },
        new() { Name = "Success", Pattern = "success|ok|passed|ready", IsRegex = true, ForegroundHex = "#50FA7B", Bold = false },
        new() { Name = "Info", Pattern = "info|notice", IsRegex = true, ForegroundHex = "#8BE9FD", Bold = false }
    ];
}

public record DisplayFilter
{
    public string IncludeText { get; init; } = string.Empty;
    public string ExcludeText { get; init; } = string.Empty;
    public bool IsRegex { get; init; }
    public bool CaseSensitive { get; init; }
    public bool RxOnly { get; init; }
    public bool TxOnly { get; init; }

    public bool IsActive => 
        !string.IsNullOrEmpty(IncludeText) || 
        !string.IsNullOrEmpty(ExcludeText) || 
        RxOnly || 
        TxOnly;
}

