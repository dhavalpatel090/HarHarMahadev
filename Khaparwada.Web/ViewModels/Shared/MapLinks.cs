using System.Globalization;

namespace Khaparwada.Web.ViewModels.Shared;

/// <summary>
/// Links to outside map apps, made from the coordinates and never stored, so they always agree with the pin.
/// Invariant culture: a decimal comma would break the URL.
/// </summary>
public static class MapLinks
{
    public static string? Directions(decimal? latitude, decimal? longitude) =>
        latitude is { } lat && longitude is { } lng
            ? string.Create(CultureInfo.InvariantCulture, $"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}")
            : null;

    public static string? OpenStreetMap(decimal? latitude, decimal? longitude) =>
        latitude is { } lat && longitude is { } lng
            ? string.Create(CultureInfo.InvariantCulture, $"https://www.openstreetmap.org/?mlat={lat}&mlon={lng}#map=17/{lat}/{lng}")
            : null;
}
