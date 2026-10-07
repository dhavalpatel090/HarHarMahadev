using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

[Route("admin/site-settings")]
[Authorize(Policy = Policies.AdminOnly)]
public class SiteSettingsController(ISettingsAdminService settings) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await settings.GetSiteSettingsAsync(cancellationToken));

    [HttpPost("")]
    [UploadLimit(images: 1, videos: 1)] // a photo and a video can arrive in one form
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
