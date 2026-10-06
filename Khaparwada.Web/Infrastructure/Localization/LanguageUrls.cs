namespace Khaparwada.Web.Infrastructure.Localization;

/// <summary>The URL of the current page in English or in Gujarati (language switcher, hreflang links).</summary>
public static class LanguageUrls
{
    /// <summary>App-relative URL of the current page in the requested language, e.g. "/places" or "/gu/places".</summary>
    public static string PathFor(HttpRequest request, bool gujarati, bool includeQuery = true)
    {
        // PathBase may hold a virtual-directory part as well as our "/gu"; remove only our part.
        var basePath = request.PathBase.Value ?? string.Empty;
        if (SiteLanguages.IsGujaratiRequest(request.HttpContext))
        {
            basePath = basePath[..^SiteLanguages.GujaratiPathPrefix.Length];
        }

        var path = request.Path.HasValue ? request.Path.Value! : "/";

        // The Gujarati home page is "/gu" (no trailing slash), the same URL that link generation produces,
        // so canonical and hreflang links point at exactly one address.
        var url = gujarati
            ? basePath + SiteLanguages.GujaratiPathPrefix + (path == "/" ? string.Empty : path)
            : basePath + path;
        return includeQuery ? url + request.QueryString : url;
    }
}
