using System.Diagnostics;
using Khaparwada.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

/// <summary>
/// Friendly error pages in the site's layout. [Route] rather than [HttpGet]: the error pages are re-executed
/// with the original request's method, so a failed POST must reach them too.
/// </summary>
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorController : Controller
{
    [Route("error/{statusCode:int:range(400,599)}")]
    public IActionResult Status(int statusCode)
    {
        Response.StatusCode = statusCode;
        return View(statusCode == StatusCodes.Status404NotFound ? "NotFound" : "Status", statusCode);
    }

    [Route("error")]
    public IActionResult Exception()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
