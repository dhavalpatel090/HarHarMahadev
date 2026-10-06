using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>A "Then vs Now" pair of photos of the same spot, shown with a before/after slider.</summary>
public class ThenNowComparison : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? TitleGu { get; set; }
    public string? Caption { get; set; }
    public string? CaptionGu { get; set; }
    public string? ThenImagePath { get; set; }
    public string? NowImagePath { get; set; }
    public string ThenLabel { get; set; } = string.Empty;   // e.g. "1980s"
    public string? ThenLabelGu { get; set; }
    public string NowLabel { get; set; } = string.Empty;    // e.g. "Today"
    public string? NowLabelGu { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
