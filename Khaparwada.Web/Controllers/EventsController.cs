using System.Text;
using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Events;
using Khaparwada.Web.ViewModels.Events;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

public class EventsController(IEventService events) : Controller
{
    /// <summary>/events (upcoming), /events?show=past, with &amp;page=2.</summary>
    [HttpGet("events", Name = RouteNames.Events)]
    public async Task<IActionResult> Index(string? show, int? page, CancellationToken cancellationToken)
    {
        var view = string.Equals(show, "past", StringComparison.OrdinalIgnoreCase) ? EventListView.Past : EventListView.Upcoming;
        return View(await events.GetListAsync(view, page, cancellationToken));
    }

    [HttpGet("events/{slug}", Name = RouteNames.EventDetails)]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var model = await events.GetDetailsAsync(slug, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        // SQL Server finds "/events/Sample-Diwali" too; send it to the one official address.
        if (!string.Equals(model.Slug, slug, StringComparison.Ordinal))
        {
            return RedirectToRoutePermanent(RouteNames.EventDetails, new { slug = model.Slug });
        }

        return View(model);
    }

    /// <summary>The address in the original plan (/Events/Details/12): a permanent redirect to /events/{slug}.</summary>
    [HttpGet("events/details/{id:int}")]
    public async Task<IActionResult> LegacyDetails(int id, CancellationToken cancellationToken)
    {
        var slug = await events.GetSlugAsync(id, cancellationToken);
        return slug is null ? NotFound() : RedirectToRoutePermanent(RouteNames.EventDetails, new { slug });
    }

    /// <summary>"Add to calendar": the event as an .ics file, which phones open in their calendar app.</summary>
    [HttpGet("events/{slug}/calendar.ics", Name = RouteNames.EventCalendar)]
    public async Task<IActionResult> Calendar(string slug, CancellationToken cancellationToken)
    {
        var pageUrl = Url.RouteUrl(RouteNames.EventDetails, new { slug }, Request.Scheme);
        var file = pageUrl is null ? null : await events.GetCalendarAsync(slug, new Uri(pageUrl), cancellationToken);
        return file is null
            ? NotFound()
            : File(Encoding.UTF8.GetBytes(file.Content), "text/calendar; charset=utf-8", file.FileName);
    }
}
