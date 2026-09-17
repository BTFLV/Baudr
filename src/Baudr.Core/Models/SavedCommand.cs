namespace Baudr.Core.Models;

public record SavedCommand
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public SendMode Mode { get; init; } = SendMode.Text;
    public LineEnding LineEnding { get; init; } = LineEnding.CrLf;
    public string CustomLineEnding { get; init; } = string.Empty;
    public string? Shortcut { get; init; }
    public string? Description { get; init; }
    public string Group { get; init; } = "General";
    public string? ColorHex { get; init; }
    public int SortOrder { get; init; }
}

