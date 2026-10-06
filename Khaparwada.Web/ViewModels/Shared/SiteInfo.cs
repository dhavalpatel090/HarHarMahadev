using System.Globalization;

namespace Khaparwada.Web.ViewModels.Shared;

/// <summary>Website and village details in the current language, for the layout, footer and Home page.</summary>
public sealed class SiteInfo
{
    public required string SiteName { get; init; }
    public required string Tagline { get; init; }
    public string? MetaDescription { get; init; }

    public required string VillageName { get; init; }
    public required string VillageIntro { get; init; }
    public int? Population { get; init; }
    public int? Households { get; init; }
    public decimal? AreaSqKm { get; init; }
    public string? StatsSource { get; init; }
    public string? Taluka { get; init; }
    public string? District { get; init; }
    public string? State { get; init; }
    public string? PinCode { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public int MapZoom { get; init; }

    public string? OfficeEmail { get; init; }

    /// <summary>Only set when the admin has chosen to show the office phone publicly.</summary>
    public string? OfficePhone { get; init; }
    public string? OfficeAddress { get; init; }

    public string? HeroImageUrl { get; init; }
    public string? HeroVideoUrl { get; init; }

    public required IReadOnlyList<SocialLink> SocialLinks { get; init; }
    public required IReadOnlyList<ExternalLink> FooterLinks { get; init; }

    public bool HasCoordinates => Latitude is not null && Longitude is not null;

    /// <summary>Google Maps directions to the village centre. Invariant culture: a decimal comma would break the URL.</summary>
    public string? DirectionsUrl => HasCoordinates
        ? string.Create(CultureInfo.InvariantCulture, $"https://www.google.com/maps/dir/?api=1&destination={Latitude},{Longitude}")
        : null;

    /// <summary>"Taluka, District, State", leaving out whatever is not filled in.</summary>
    public string Location => string.Join(", ", new[] { Taluka, District, State }.Where(part => !string.IsNullOrWhiteSpace(part)));
}

public sealed record SocialLink(string Name, string Url, string BrandIcon);

public sealed record ExternalLink(string Title, string Url);
