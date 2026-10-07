using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Places;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class PlacesController(IPlaceService places) : Controller
{
    /// <summary>/places, /places?category=temples, /places?q=lake&amp;page=2.</summary>
    [HttpGet("places", Name = RouteNames.Places)]
    public async Task<IActionResult> Index(string? category, string? q, int? page, CancellationToken cancellationToken) =>
        View(await places.GetListAsync(category, q, page, cancellationToken));

    [HttpGet("places/{slug}", Name = RouteNames.PlaceDetails)]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var model = await places.GetDetailsAsync(slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        // SQL Server compares text without regard to case, so "/places/Village-Temple" finds the place too;
        // send it to the one official address, so search engines do not count two pages.
        if (!string.Equals(model.Slug, slug, StringComparison.Ordinal))
        {
            return RedirectToRoutePermanent(RouteNames.PlaceDetails, new { slug = model.Slug });
        }

        return View(model);
    }

    /// <summary>The address in the original plan (/Places/Details/12): a permanent redirect to /places/{slug}.</summary>
    [HttpGet("places/details/{id:int}")]
    public async Task<IActionResult> LegacyDetails(int id, CancellationToken cancellationToken)
    {
        var slug = await places.GetSlugAsync(id, cancellationToken);
        return slug is null ? NotFound() : RedirectToRoutePermanent(RouteNames.PlaceDetails, new { slug });
    }
}
