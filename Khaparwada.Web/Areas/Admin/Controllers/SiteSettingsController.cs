using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

[Route("admin/site-settings")]
[Authorize(Policy = Policies.AdminOnly)]
public class SiteSettingsController(ISettingsAdminService settings) : AdminControllerBase
{
    // A photo (10 MB) and a video (25 MB) can arrive in one form. The services enforce the exact limits from
    // Storage settings; this only caps the whole request. Raise it if you raise those limits.
    private const long MaxRequestBytes = 40L * 1024 * 1024;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await settings.GetSiteSettingsAsync(cancellationToken));

    [HttpPost("")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Index(SiteSettingsInputModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await settings.SaveSiteSettingsAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage("Site settings saved. The website shows the changes immediately.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        // Show the form again with the photo and video that are currently live.
        var current = await settings.GetSiteSettingsAsync(cancellationToken);
        input.CurrentHeroImageUrl = current.CurrentHeroImageUrl;
        input.CurrentHeroVideoUrl = current.CurrentHeroVideoUrl;
        return View(input);
    }
}
