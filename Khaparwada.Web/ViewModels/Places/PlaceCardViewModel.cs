using Khaparwada.Web.Infrastructure.Media;

namespace Khaparwada.Web.ViewModels.Places;

public sealed class PlaceCardViewModel
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Summary { get; init; }
    public required string CategoryName { get; init; }
    public required string CategoryIcon { get; init; }
    public required string CategoryColor { get; init; }
    public string? ImagePath { get; init; }

    public string? ImageUrl => MediaUrls.Image(ImagePath);
}

public sealed class CategoryTileViewModel
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public required string Color { get; init; }
    public int PlaceCount { get; init; }
}
