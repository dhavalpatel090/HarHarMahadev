using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Gallery;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class GalleryController(IGalleryService gallery) : Controller
{
    /// <summary>/gallery, /gallery?category=navratri, with &amp;page=2.</summary>
    [HttpGet("gallery", Name = RouteNames.Gallery)]
    public async Task<IActionResult> Index(string? category, int? page, CancellationToken cancellationToken) =>
        View(await gallery.GetListAsync(category, page, cancellationToken));

    /// <summary>/gallery/{slug}, with ?page=2 for the next photos.</summary>
    [HttpGet("gallery/{slug}", Name = RouteNames.AlbumDetails)]
    public async Task<IActionResult> Album(string slug, int? page, CancellationToken cancellationToken)
    {
        var model = await gallery.GetAlbumAsync(slug, page, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        // SQL Server finds "/gallery/Navratri-Photos" too; send it to the one official address (keeping the page).
        if (!string.Equals(model.Slug, slug, StringComparison.Ordinal))
        {
            var current = model.Pagination.Page;
            return RedirectToRoutePermanent(RouteNames.AlbumDetails, new { slug = model.Slug, page = current > 1 ? current : (int?)null });
        }

        return View(model);
    }
}
