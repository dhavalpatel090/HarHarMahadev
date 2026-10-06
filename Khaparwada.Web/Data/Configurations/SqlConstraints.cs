using Khaparwada.Web.Models.Enums;

namespace Khaparwada.Web.Data.Configurations;

/// <summary>
/// SQL used by several entity configurations for filtered indexes and CHECK constraints.
/// CHECK constraints are the database's last line of defence: they hold even if a bug in the code,
/// or a manual SQL edit, tries to save invalid data.
/// </summary>
internal static class SqlConstraints
{
    /// <summary>Filter for unique slug indexes, so a deleted page doesn't block its URL from being reused.</summary>
    public const string NotDeleted = "[IsDeleted] = 0";

    /// <summary>Single-row settings tables: the only allowed id is 1.</summary>
    public const string SingleRow = "[Id] = 1";

    /// <summary>
    /// Latitude and Longitude are both empty or both valid. The explicit IS NOT NULL checks matter:
    /// SQL Server accepts a row when a CHECK evaluates to UNKNOWN, which is what comparing a NULL produces.
    /// </summary>
    public const string Coordinates =
        "([Latitude] IS NULL AND [Longitude] IS NULL) OR " +
        "([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL " +
        "AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)";

    /// <summary>Indian PIN code: six digits, not starting with 0.</summary>
    public const string PinCode = "[PinCode] IS NULL OR [PinCode] LIKE '[1-9][0-9][0-9][0-9][0-9][0-9]'";

    /// <summary>"#RRGGBB" only, because the value is written into the page's styles.</summary>
    public static string HexColor(string column) =>
        $"[{column}] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'";

    /// <summary>Links must be http(s). This blocks "javascript:" links, which HTML encoding alone does not stop.</summary>
    public static string HttpUrl(string column) =>
        $"[{column}] IS NULL OR [{column}] LIKE 'https://%' OR [{column}] LIKE 'http://%'";

    /// <summary>Content can only be Approved or Published once consent is recorded.</summary>
    public static string ConsentRequired(string consentColumn) =>
        $"[Status] NOT IN (N'{nameof(ModerationStatus.Approved)}', N'{nameof(ModerationStatus.Published)}') " +
        $"OR [{consentColumn}] = 1";
}
