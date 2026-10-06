namespace Khaparwada.Web.Models.Enums;

/// <summary>
/// Review workflow for content that needs approval before it becomes public.
/// Only <see cref="Published"/> items are visible on the public site.
/// Stored as text in the database, so reordering the members is safe.
/// </summary>
public enum ModerationStatus
{
    Pending,
    UnderReview,
    Approved,
    Published,
    Rejected
}
