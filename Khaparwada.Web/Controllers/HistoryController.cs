using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.History;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class HistoryController(IHistoryService history) : Controller
{
    [HttpGet("history", Name = RouteNames.History)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await history.GetPageAsync(cancellationToken));
}
