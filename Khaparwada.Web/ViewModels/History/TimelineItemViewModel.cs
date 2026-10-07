namespace Khaparwada.Web.ViewModels.History;

/// <summary>One timeline entry: on the History page in full, on Home and About as a shortened highlight.</summary>
public sealed record TimelineItemViewModel
{
    public required int Id { get; init; }
    public required string YearLabel { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? ImagePath { get; init; }
    public bool IsHighlighted { get; init; }

    /// <summary>The entry's id on the History page, so Home and About can link straight to it.</summary>
    public string Anchor => $"year-{Id}";
}
