using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>"Then and now" photo pairs (Home and /history#then-and-now). Admins and Editors.</summary>
[Route("admin/then-and-now")]
public class ThenNowController(IThenNowAdminService pairs) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await pairs.ListAsync(cancellationToken));

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var existing = await pairs.ListAsync(cancellationToken);
        var next = existing.Count == 0 ? 10 : existing.Max(pair => pair.SortOrder) + 10;
        return View("Edit", new ThenNowInputModel { SortOrder = next });
    }

    [HttpPost("new")]
    [UploadLimit(images: 2)]
    public Task<IActionResult> Create(ThenNowInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, "Photo pair added.", cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await pairs.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}")]
    [UploadLimit(images: 2)]
    public Task<IActionResult> Edit(int id, ThenNowInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, "Photo pair saved.", cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await pairs.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.Title, "photo pair"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await pairs.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Photo pair deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(ThenNowInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await pairs.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage(successMessage);
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        if (input.Id is { } id && await pairs.GetAsync(id, cancellationToken) is { } current)
        {
            input.CurrentThenImageUrl = current.CurrentThenImageUrl;
            input.CurrentNowImageUrl = current.CurrentNowImageUrl;
        }

        return View("Edit", input);
    }
}
