using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>Events (public /events and the Home page). Admins and Editors.</summary>
[Route("admin/events")]
public class EventsController(IEventAdminService events) : AdminControllerBase
{
    /// <summary>/admin/events (upcoming), ?show=past, ?show=all, with &amp;q=garba&amp;page=2. Named for the pager.</summary>
    [HttpGet("", Name = "admin-events")]
    public async Task<IActionResult> Index(EventListFilter? show, string? q, int? page, CancellationToken cancellationToken)
    {
        // Enum binding also accepts numbers (?show=7), so anything that is not a real tab shows "Upcoming".
        var filter = show is { } chosen && Enum.IsDefined(chosen) ? chosen : EventListFilter.Upcoming;
        return View(await events.ListAsync(filter, q, page, cancellationToken));
    }

    /// <summary>/admin/events/new, or ?copy=12 to start from an existing event (yearly festivals).</summary>
    [HttpGet("new")]
    public async Task<IActionResult> Create(int? copy, CancellationToken cancellationToken)
    {
        var model = copy is { } id ? await events.GetCopyAsync(id, cancellationToken) : null;
        return await EditViewAsync(model ?? new EventInputModel(), cancellationToken);
    }

    [HttpPost("new")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Create(EventInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, "Event added.", cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await events.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : await EditViewAsync(model, cancellationToken);
    }

    [HttpPost("{id:int}")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Edit(int id, EventInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, "Event saved.", cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await events.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.Title, "event"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await events.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Event deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(EventInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var outcome = await events.SaveAsync(input, cancellationToken);
            if (outcome.Result.Succeeded)
            {
                // Open the tab the event is on, so an event that is already over does not seem to vanish.
                SetStatusMessage(outcome.IsOver ? successMessage + " It is already over, so it is listed under “Past”." : successMessage);
                return outcome.IsOver
                    ? RedirectToAction(nameof(Index), new { show = "past" })
                    : RedirectToAction(nameof(Index));
            }

            AddErrors(outcome.Result);
        }

        if (input.Id is { } id && await events.GetAsync(id, cancellationToken) is { } saved)
        {
            input.CurrentImageUrl = saved.CurrentImageUrl;
        }

        return await EditViewAsync(input, cancellationToken);
    }

    private async Task<IActionResult> EditViewAsync(EventInputModel model, CancellationToken cancellationToken)
    {
        model.Places = await events.GetPlacesAsync(cancellationToken);
        return View("Edit", model);
    }
}
