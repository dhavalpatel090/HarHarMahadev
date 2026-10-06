using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// Website-wide settings that the admin edits. Exactly one row (Id = 1).
/// Technical settings (map tiles, storage paths) live in appsettings.json instead, because they
/// change per server, not per editorial decision.
/// </summary>
public class SiteSetting : BaseEntity
{
    public const int SingletonId = 1;

    public string SiteName { get; set; } = string.Empty;
    public string? SiteNameGu { get; set; }
    public string Tagline { get; set; } = string.Empty;
    public string? TaglineGu { get; set; }

    public string? OfficeEmail { get; set; }
    public string? OfficePhone { get; set; }

    /// <summary>The office phone is shown publicly only when the admin switches this on.</summary>
    public bool ShowOfficePhone { get; set; }
    public string? OfficeAddress { get; set; }
    public string? OfficeAddressGu { get; set; }

    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? WhatsAppUrl { get; set; }

    public string? HeroImagePath { get; set; }
    public string? HeroVideoPath { get; set; }
    public string? DefaultMetaDescription { get; set; }
    public string? DefaultMetaDescriptionGu { get; set; }
}
