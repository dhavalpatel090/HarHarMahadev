using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Enums;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// A notice-board item. A notice is "active" while it is published, <see cref="PublishedAt"/> has
/// passed and <see cref="ExpiresAt"/> has not; that is computed when the page is built, never stored.
/// </summary>
public class Notice : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;
    public string? TitleGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionGu { get; set; }
    public NoticeCategory Category { get; set; } = NoticeCategory.General;
    public NoticePriority Priority { get; set; } = NoticePriority.Normal;
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? ImagePath { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }             // original file name, shown on the download link
    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
