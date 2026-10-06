namespace Khaparwada.Web.ViewModels.Memories;

public sealed class MemoryCardViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Excerpt { get; init; }
    public string? EraLabel { get; init; }
    public required string SubmitterName { get; init; }
}
