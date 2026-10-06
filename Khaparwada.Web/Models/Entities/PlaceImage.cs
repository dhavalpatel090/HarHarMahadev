using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>A photo in a place's gallery. Width and Height let the browser reserve space before the image loads.</summary>
public class PlaceImage : BaseEntity
{
    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;

    public string ImagePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Caption { get; set; }
    public string? CaptionGu { get; set; }
    public int SortOrder { get; set; }
}
