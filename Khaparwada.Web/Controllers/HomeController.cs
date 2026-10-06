using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Home;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class HomeController(IHomeService homeService) : Controller
{
    [HttpGet("", Name = RouteNames.Home)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await homeService.GetAsync(cancellationToken));
    }

    [HttpGet("privacy", Name = RouteNames.Privacy)]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("terms", Name = RouteNames.Terms)]
    public IActionResult Terms()
    {
        return View();
    }
}
