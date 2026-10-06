using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// A village event. "Upcoming" and "past" are worked out from <see cref="StartsAt"/> and
/// <see cref="EndsAt"/> when the page is built, never stored, so they are always correct.
/// </summary>
public class Event : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? TitleGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? SummaryGu { get; set; }
    public string? Description { get; set; }
    public string? DescriptionGu { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public bool IsAllDay { get; set; }

    /// <summary>Optional venue from the Places list; <see cref="LocationText"/> covers anywhere else.</summary>
    public int? PlaceId { get; set; }
    public Place? Place { get; set; }
    public string? LocationText { get; set; }
    public string? LocationTextGu { get; set; }

    public string? ImagePath { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<GalleryAlbum> Albums { get; set; } = new List<GalleryAlbum>();
}
