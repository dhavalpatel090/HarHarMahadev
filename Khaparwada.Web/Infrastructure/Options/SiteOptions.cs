using System.ComponentModel.DataAnnotations;

namespace Khaparwada.Web.Infrastructure.Options;

/// <summary>Settings from the "Site" section of appsettings.json.</summary>
public sealed class SiteOptions
{
    public const string SectionName = "Site";

    /// <summary>
    /// Time zone for showing dates and deciding what is "upcoming" (hosting servers usually run on UTC).
    /// An IANA id such as "Asia/Kolkata" works on both Windows and Linux.
    /// </summary>
    [Required]
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
}
