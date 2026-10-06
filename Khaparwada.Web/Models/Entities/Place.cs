using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// A place in the village (temple, school, office, lake, …). Places with coordinates appear on the map.
/// Google Maps / OpenStreetMap links are generated from <see cref="Latitude"/> and <see cref="Longitude"/>,
/// never stored, so they can't disagree with the coordinates.
/// </summary>
public class Place : BaseEntity, ISoftDeletable
{
    public int CategoryId { get; set; }
    public PlaceCategory Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? NameGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;     // cards, map pop-ups, meta description
    public string? SummaryGu { get; set; }
    public string? Description { get; set; }
    public string? DescriptionGu { get; set; }
    public string? Address { get; set; }
    public string? AddressGu { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? MainImagePath { get; set; }

    // Public contact details of the place itself (e.g. the panchayat office), never a private person's number.
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Timings { get; set; }                    // e.g. darshan or office hours
    public string? TimingsGu { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<PlaceImage> Images { get; set; } = new List<PlaceImage>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
