using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.About;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class AboutController(IAboutService about) : Controller
{
    [HttpGet("about", Name = RouteNames.About)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await about.GetPageAsync(cancellationToken));
}
