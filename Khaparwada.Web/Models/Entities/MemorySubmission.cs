using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Enums;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// A memory, story or old photo sent in by a resident. It stays private until an admin moves it
/// through Pending → UnderReview → Approved → Published. The text is stored once, in the language
/// the resident wrote it in, so there are no Gujarati twin columns here.
/// </summary>
public class MemorySubmission : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string Story { get; set; } = string.Empty;
    public string? EraLabel { get; set; }                   // e.g. "1970s"
    public string SubmitterName { get; set; } = string.Empty;

    /// <summary>Phone or email so the admin can follow up. Admin-only; never shown publicly.</summary>
    public string? SubmitterContact { get; set; }
    public bool ConsentToPublish { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;

    /// <summary>Internal note from the reviewer. Admin-only.</summary>
    public string? ReviewNote { get; set; }
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Assigned when the memory is published.</summary>
    public string? Slug { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<MemoryImage> Images { get; set; } = new List<MemoryImage>();
}
