namespace Khaparwada.Web.ViewModels.Map;

public sealed class MapViewModel
{
    /// <summary>Village centre; when empty the map zooms to fit the markers instead.</summary>
    public decimal? CenterLatitude { get; init; }
    public decimal? CenterLongitude { get; init; }
    public int Zoom { get; init; }
    public required IReadOnlyList<MapMarkerViewModel> Markers { get; init; }
}

public sealed class MapMarkerViewModel
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Summary { get; init; }
    public required string Category { get; init; }
    public required string CategorySlug { get; init; }

    /// <summary>The category's icon as stored ("fa-solid fa-gopuram"); IconNames.ToSymbol turns it into a sprite id.</summary>
    public required string Icon { get; init; }
    public required string Color { get; init; }
    public string? ImagePath { get; init; }
    public required decimal Latitude { get; init; }
    public required decimal Longitude { get; init; }
}

/// <summary>
/// One map on a page (Views/Shared/_MapCanvas). Lazy maps load Leaflet only when they scroll near the screen;
/// the map page loads it at once. Popups = false for a place's own page, where "View details" would link to
/// the page itself. Category: frame this category's places first.
/// </summary>
public sealed record MapCanvasViewModel(
    string Id, MapViewModel Map, string AriaLabel, bool Lazy = true, string? CssClass = null, bool Popups = true, string? Category = null);

/// <summary>The /map page: the map, a filter per category, and the same places as a list.</summary>
public sealed class MapPageViewModel
{
    public required MapViewModel Map { get; init; }
    public required IReadOnlyList<MapCategoryViewModel> Categories { get; init; }

    /// <summary>A category to show alone at first (/map?category=temples), if any.</summary>
    public string? SelectedCategory { get; init; }
}

public sealed record MapCategoryViewModel(string Slug, string Name, string Icon, string Color, IReadOnlyList<MapMarkerViewModel> Places);
