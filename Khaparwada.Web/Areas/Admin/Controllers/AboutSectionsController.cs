using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>The sections of the public About page. Admins and Editors.</summary>
[Route("admin/about")]
public class AboutSectionsController(IAboutSectionAdminService sections) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await sections.ListAsync(cancellationToken));

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var existing = await sections.ListAsync(cancellationToken);
        var next = existing.Count == 0 ? 10 : existing.Max(section => section.SortOrder) + 10;
        return View("Edit", new AboutSectionInputModel { SortOrder = next });
    }

    [HttpPost("new")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Create(AboutSectionInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, "Section added.", cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await sections.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Edit(int id, AboutSectionInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, "Section saved.", cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await sections.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.Title, "section"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await sections.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Section deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(AboutSectionInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await sections.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage(successMessage);
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        // Show the form again with the photo that is currently saved (a browser never re-sends a chosen file).
        if (input.Id is { } id)
        {
            input.CurrentImageUrl = (await sections.GetAsync(id, cancellationToken))?.CurrentImageUrl;
        }

        return View("Edit", input);
    }
}
