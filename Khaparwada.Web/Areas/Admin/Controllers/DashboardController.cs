using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

[Route("admin")]
public class DashboardController(IDashboardService dashboard) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboard.GetAsync(User.DisplayName(), User.IsInRole(AppRoles.Admin), cancellationToken));
}
