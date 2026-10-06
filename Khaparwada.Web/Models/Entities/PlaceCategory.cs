using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>Category of place (Temples, Schools, …); also decides the map marker's icon and colour.</summary>
public class PlaceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;   // Font Awesome classes, e.g. "fa-solid fa-gopuram"
    public string MarkerColor { get; set; } = string.Empty; // "#RRGGBB"

    /// <summary>When true, Home shows how many published places this category has.</summary>
    public bool ShowInStats { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Place> Places { get; set; } = new List<Place>();
}
