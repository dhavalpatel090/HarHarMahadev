using System.ComponentModel.DataAnnotations;

namespace Khaparwada.Web.Infrastructure.Options;

/// <summary>
/// Settings from the "Map" section of appsettings.json. The default is OpenStreetMap's free tile server,
/// which asks for light use and visible attribution; to switch providers, change these settings only.
/// </summary>
public sealed class MapOptions
{
    public const string SectionName = "Map";

    [Required, Url]
    public string TileUrl { get; set; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    /// <summary>Credit shown on the map (may contain a link).</summary>
    [Required]
    public string Attribution { get; set; } =
        "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors";

    [Range(1, 22)]
    public int MaxZoom { get; set; } = 19;
}
