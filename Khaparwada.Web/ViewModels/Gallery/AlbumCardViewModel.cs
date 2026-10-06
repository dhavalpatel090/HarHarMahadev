using Khaparwada.Web.Infrastructure.Media;

namespace Khaparwada.Web.ViewModels.Gallery;

public sealed class AlbumCardViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string CategoryName { get; init; }
    public DateOnly? TakenOn { get; init; }
    public int PhotoCount { get; init; }
    public string? CoverImagePath { get; init; }

    public string? CoverImageUrl => MediaUrls.Image(CoverImagePath);
}
