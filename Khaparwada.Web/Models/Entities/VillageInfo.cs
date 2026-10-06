using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// Facts about the village shown on Home, About and the map. Exactly one row (Id = 1).
/// Properties ending in "Gu" hold the Gujarati text; when a Gujarati value is empty, the English one is shown.
/// </summary>
public class VillageInfo : BaseEntity
{
    public const int SingletonId = 1;

    public string Name { get; set; } = string.Empty;
    public string? NameGu { get; set; }
    public string ShortIntro { get; set; } = string.Empty;
    public string? ShortIntroGu { get; set; }

    // Statistics typed in by the admin. Counts of schools, temples and places are not stored here:
    // they are computed from the Places table so they can never go stale.
    public int? Population { get; set; }
    public int? Households { get; set; }
    public decimal? AreaSqKm { get; set; }
    public string? StatsSource { get; set; }            // e.g. "Gram Panchayat records, 2025"
    public string? StatsSourceGu { get; set; }

    public string? Taluka { get; set; }
    public string? TalukaGu { get; set; }
    public string? District { get; set; }
    public string? DistrictGu { get; set; }
    public string? State { get; set; }
    public string? StateGu { get; set; }
    public string? PinCode { get; set; }

    // Centre of the village map; empty until the admin enters the real coordinates.
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int MapZoom { get; set; } = 15;
}
