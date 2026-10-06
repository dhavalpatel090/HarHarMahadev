using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>Gallery category (Old Khaparwada, Navratri, Diwali, …).</summary>
public class GalleryCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<GalleryAlbum> Albums { get; set; } = new List<GalleryAlbum>();
}
