using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// One point on the village history timeline. <see cref="SortYear"/> orders the timeline;
/// <see cref="YearLabel"/> is what visitors see, so approximate dates ("1950s", "Before 1947")
/// don't need fake precision.
/// </summary>
public class HistoryEntry : BaseEntity, ISoftDeletable
{
    public int SortYear { get; set; }
    public string YearLabel { get; set; } = string.Empty;
    public string? YearLabelGu { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleGu { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? DescriptionGu { get; set; }
    public string? ImagePath { get; set; }

    /// <summary>Highlighted entries also appear on Home and as "Milestones" on the About page.</summary>
    public bool IsHighlighted { get; set; }
    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
