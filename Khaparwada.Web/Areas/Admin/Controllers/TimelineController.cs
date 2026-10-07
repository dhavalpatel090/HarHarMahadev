using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>The history timeline (public /history). Admins and Editors.</summary>
[Route("admin/history")]
public class TimelineController(ITimelineAdminService timeline) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await timeline.ListAsync(cancellationToken));

    [HttpGet("new")]
    public IActionResult Create() => View("Edit", new TimelineEntryInputModel());

    [HttpPost("new")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Create(TimelineEntryInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, "Entry added to the timeline.", cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await timeline.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Edit(int id, TimelineEntryInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, "Entry saved.", cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await timeline.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.Title, "timeline entry"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await timeline.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Entry deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(TimelineEntryInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await timeline.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage(successMessage);
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        if (input.Id is { } id)
        {
            input.CurrentImageUrl = (await timeline.GetAsync(id, cancellationToken))?.CurrentImageUrl;
        }

        return View("Edit", input);
    }
}
