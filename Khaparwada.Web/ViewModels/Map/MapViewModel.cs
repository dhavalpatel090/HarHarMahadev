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

    /// <summary>Symbol id in the SVG sprite.</summary>
    public required string Icon { get; init; }
    public required string Color { get; init; }
    public required decimal Latitude { get; init; }
    public required decimal Longitude { get; init; }
}
