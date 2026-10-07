using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.People;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class PeopleController(IPeopleService people) : Controller
{
    /// <summary>/people, /people?category=teachers, /people?q=patel&amp;page=2.</summary>
    [HttpGet("people", Name = RouteNames.People)]
    public async Task<IActionResult> Index(string? category, string? q, int? page, CancellationToken cancellationToken) =>
        View(await people.GetListAsync(category, q, page, cancellationToken));

    [HttpGet("people/{slug}", Name = RouteNames.PersonDetails)]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var model = await people.GetDetailsAsync(slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        // One official address per profile (SQL Server finds "/people/Sample-Teacher" too).
        if (!string.Equals(model.Slug, slug, StringComparison.Ordinal))
        {
            return RedirectToRoutePermanent(RouteNames.PersonDetails, new { slug = model.Slug });
        }

        return View(model);
    }
}
