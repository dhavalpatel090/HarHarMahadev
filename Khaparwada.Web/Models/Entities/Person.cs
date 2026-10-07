using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Enums;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// A community profile. By design there are no phone, address or birth-date fields:
/// data that is never stored can never leak. A profile can only be Approved or Published when
/// <see cref="HasConsent"/> is true (enforced by a database check constraint).
/// </summary>
public class Person : BaseEntity, ISoftDeletable
{
    public int CategoryId { get; set; }
    public PersonCategory Category { get; set; } = null!;

    public string FullName { get; set; } = string.Empty;
    public string? FullNameGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Profession { get; set; } = string.Empty;
    public string? ProfessionGu { get; set; }
    public string ShortBio { get; set; } = string.Empty;
    public string? ShortBioGu { get; set; }
    public string? Achievement { get; set; }
    public string? AchievementGu { get; set; }
    public string? Contribution { get; set; }
    public string? ContributionGu { get; set; }
    public string? PhotoPath { get; set; }

    /// <summary>The person (or their family) has agreed to this profile being public.</summary>
    public bool HasConsent { get; set; }

    /// <summary>Admin-only: how consent was given, e.g. "Signed form, kept at the panchayat office". Never shown publicly.</summary>
    public string? ConsentNote { get; set; }

    /// <summary>When consent was recorded, so a later question ("did I agree?") can be answered.</summary>
    public DateTimeOffset? ConsentRecordedAt { get; set; }
    public bool IsFeatured { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>Admin-only: the reviewer's note, e.g. why a profile was sent back. Never shown publicly.</summary>
    public string? ReviewNote { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
