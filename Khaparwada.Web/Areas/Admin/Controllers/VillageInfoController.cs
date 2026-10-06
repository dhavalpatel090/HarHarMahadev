using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

[Route("admin/village-info")]
[Authorize(Policy = Policies.AdminOnly)]
public class VillageInfoController(ISettingsAdminService settings) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await settings.GetVillageInfoAsync(cancellationToken));

    [HttpPost("")]
    public async Task<IActionResult> Index(VillageInfoInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        await settings.SaveVillageInfoAsync(input, cancellationToken);
        SetStatusMessage("Village information saved. The website shows the changes immediately.");
        return RedirectToAction(nameof(Index));
    }
}
