using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Places;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class MapController(IPlaceService places) : Controller
{
    /// <summary>/map, or /map?category=temples to show one category at first.</summary>
    [HttpGet("map", Name = RouteNames.Map)]
    public async Task<IActionResult> Index(string? category, CancellationToken cancellationToken) =>
        View(await places.GetMapPageAsync(category, cancellationToken));
}
