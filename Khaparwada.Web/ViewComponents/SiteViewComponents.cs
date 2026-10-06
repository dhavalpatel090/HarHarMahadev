using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Options;
using Khaparwada.Web.Services.Notices;
using Khaparwada.Web.Services.Site;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.ViewComponents;

/// <summary>Footer: quick links, important links, village location, social links. Views/Shared/Components/SiteFooter.</summary>
public sealed class SiteFooterViewComponent(ISiteInfoService siteInfo) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() =>
        View(await siteInfo.GetAsync(HttpContext.RequestAborted));
}

/// <summary>Red bar above the header while an urgent notice is active. Renders nothing otherwise.</summary>
public sealed class AlertBarViewComponent(INoticeService notices) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var alerts = await notices.GetUrgentAlertsAsync(HttpContext.RequestAborted);
        return alerts.Count == 0 ? Content(string.Empty) : View(alerts);
    }
}

/// <summary>
/// &lt;title&gt;, description, canonical and language links, and Open Graph tags for link previews.
/// Pages set ViewData["Title"] and optionally ViewData["Description"] and ViewData["ShareImage"].
/// </summary>
public sealed class PageMetaViewComponent(ISiteInfoService siteInfo, IOptions<SiteOptions> options) : ViewComponent
{
    private const string DefaultShareImage = "/images/brand/og-default.jpg";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var site = await siteInfo.GetAsync(HttpContext.RequestAborted);
        var request = HttpContext.Request;

        // Behind a proxy the request's own host can be an internal name, so production sets Site:PublicBaseUrl.
        var baseUrl = string.IsNullOrWhiteSpace(options.Value.PublicBaseUrl)
            ? $"{request.Scheme}://{request.Host}"
            : options.Value.PublicBaseUrl.TrimEnd('/');

        var englishUrl = baseUrl + LanguageUrls.PathFor(request, gujarati: false, includeQuery: false);
        var gujaratiUrl = baseUrl + LanguageUrls.PathFor(request, gujarati: true, includeQuery: false);
        var gujarati = AppLanguage.IsGujarati;
        var pageTitle = ViewData["Title"] as string;

        return View(new PageMetaViewModel
        {
            Title = string.IsNullOrWhiteSpace(pageTitle) ? $"{site.SiteName} | {site.Tagline}" : $"{pageTitle} | {site.SiteName}",
            ShareTitle = string.IsNullOrWhiteSpace(pageTitle) ? site.SiteName : pageTitle,
            Description = ViewData["Description"] as string ?? site.MetaDescription,
            CanonicalUrl = gujarati ? gujaratiUrl : englishUrl,
            EnglishUrl = englishUrl,
            GujaratiUrl = gujaratiUrl,
            ImageUrl = baseUrl + (ViewData["ShareImage"] as string ?? DefaultShareImage),
            SiteName = site.SiteName,
            Locale = gujarati ? "gu_IN" : "en_IN",
            AlternateLocale = gujarati ? "en_IN" : "gu_IN"
        });
    }
}
