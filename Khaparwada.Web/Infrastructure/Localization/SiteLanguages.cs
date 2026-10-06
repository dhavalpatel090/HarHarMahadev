using System.Globalization;

namespace Khaparwada.Web.Infrastructure.Localization;

/// <summary>
/// The site's two languages. English pages live at "/…" and Gujarati pages at "/gu/…": each language has
/// its own URL, so search engines index both (a cookie-only switch would hide the Gujarati pages from Google).
/// </summary>
public static class SiteLanguages
{
    public const string GujaratiPathPrefix = "/gu";

    public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-IN");
    public static readonly CultureInfo Gujarati = CultureInfo.GetCultureInfo("gu-IN");

    internal const string GujaratiRequestItemKey = "Khaparwada.GujaratiRequest";

    /// <summary>True when the current request came in under "/gu".</summary>
    public static bool IsGujaratiRequest(HttpContext context) => context.Items.ContainsKey(GujaratiRequestItemKey);
}
