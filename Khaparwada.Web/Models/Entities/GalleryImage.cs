using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Enums;

namespace Khaparwada.Web.Models.Entities;

/// <summary>One gallery photo. Its upload date is <see cref="BaseEntity.CreatedAt"/>.</summary>
public class GalleryImage : BaseEntity, ISoftDeletable
{
    public int AlbumId { get; set; }
    public GalleryAlbum Album { get; set; } = null!;

    public string ImagePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Caption { get; set; }
    public string? CaptionGu { get; set; }
    public int SortOrder { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Pending;
    public string? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
