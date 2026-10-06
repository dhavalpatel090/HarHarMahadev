using Microsoft.AspNetCore.Localization;

namespace Khaparwada.Web.Infrastructure.Localization;

/// <summary>
/// Takes the language from the URL only. Cookies and the browser's Accept-Language header are deliberately
/// ignored: if they could change the language, one URL would show different languages to different
/// visitors, and search engines would index the wrong one.
/// </summary>
public sealed class LanguagePrefixCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext) =>
        SiteLanguages.IsGujaratiRequest(httpContext)
            ? Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(SiteLanguages.Gujarati.Name))
            : NullProviderCultureResult;
}
