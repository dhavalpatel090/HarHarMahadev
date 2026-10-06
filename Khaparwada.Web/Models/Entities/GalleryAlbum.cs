using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// A set of photos, e.g. "Navratri 2025". The category is stored once on the album, not on every photo.
/// The cover is simply the first photo by SortOrder; a cover-image column would create a circular
/// foreign key between albums and images.
/// </summary>
public class GalleryAlbum : BaseEntity, ISoftDeletable
{
    public int CategoryId { get; set; }
    public GalleryCategory Category { get; set; } = null!;

    /// <summary>Optional link to the event these photos are from.</summary>
    public int? EventId { get; set; }
    public Event? Event { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? TitleGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionGu { get; set; }
    public DateOnly? TakenOn { get; set; }
    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<GalleryImage> Images { get; set; } = new List<GalleryImage>();
}
