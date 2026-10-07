using Khaparwada.Web.ViewModels.Events;
using Khaparwada.Web.ViewModels.Map;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.Places;

/// <summary>/places: filter by category, search, one page of results.</summary>
public sealed class PlaceListViewModel
{
    public required IReadOnlyList<CategoryTileViewModel> Categories { get; init; }

    /// <summary>The chosen category, or null for all places.</summary>
    public CategoryTileViewModel? Category { get; init; }

    public string? Search { get; init; }
    public required IReadOnlyList<PlaceCardViewModel> Places { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>/places/{slug}.</summary>
public sealed class PlaceDetailsViewModel
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Summary { get; init; }
    public string? Description { get; init; }
    public required CategoryTileViewModel Category { get; init; }
    public string? Address { get; init; }
    public string? Timings { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? MainImagePath { get; init; }
    public required IReadOnlyList<PlacePhotoViewModel> Photos { get; init; }
    public required IReadOnlyList<EventCardViewModel> UpcomingEvents { get; init; }
    public required IReadOnlyList<PlaceCardViewModel> MorePlaces { get; init; }

    /// <summary>A small map with just this place, when it has coordinates.</summary>
    public MapViewModel? Map { get; init; }

    public bool HasCoordinates => Latitude is not null && Longitude is not null;

    /// <summary>"tel:" link: a leading + and the digits only, since spaces and brackets are not allowed there.</summary>
    public string? PhoneHref
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Phone))
            {
                return null;
            }

            var number = Phone.Trim();
            return "tel:" + (number.StartsWith('+') ? "+" : string.Empty) + new string(number.Where(char.IsAsciiDigit).ToArray());
        }
    }

    // Map links are made from the coordinates, never stored, so they always agree. Invariant culture: a decimal
    // comma would break the URL.
    public string? DirectionsUrl => HasCoordinates
        ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"https://www.google.com/maps/dir/?api=1&destination={Latitude},{Longitude}")
        : null;

    public string? OpenStreetMapUrl => HasCoordinates
        ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"https://www.openstreetmap.org/?mlat={Latitude}&mlon={Longitude}#map=17/{Latitude}/{Longitude}")
        : null;
}

public sealed record PlacePhotoViewModel(string ImagePath, int Width, int Height, string? Caption);
