namespace Khaparwada.Web.ViewModels.History;

public sealed class TimelineItemViewModel
{
    public required string YearLabel { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? ImagePath { get; init; }
}
