using System.Security.Claims;
using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>
/// Gallery albums and photos (public /gallery). Admins and Editors manage albums and upload photos; an Editor's
/// photos wait in the review queue, where only Admins decide.
/// </summary>
[Route("admin/gallery")]
public class GalleryController(IGalleryAdminService gallery) : AdminControllerBase
{
    /// <summary>/admin/gallery?category=3&amp;q=navratri&amp;page=2. Named so the shared pager can build its links.</summary>
    [HttpGet("", Name = "admin-gallery")]
    public async Task<IActionResult> Index(int? category, string? q, int? page, CancellationToken cancellationToken) =>
        View(await gallery.ListAsync(category, q, page, cancellationToken));

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        await EditViewAsync(new AlbumInputModel(), cancellationToken);

    [HttpPost("new")]
    public async Task<IActionResult> Create(AlbumInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        if (ModelState.IsValid)
        {
            var result = await gallery.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                // Straight on to the photos: an album without photos is not shown on the website.
                SetStatusMessage("Album added. Now add its photos.");
                return RedirectToAction(nameof(Photos), new { id = input.Id });
            }

            AddErrors(result);
        }

        input.Id = null;
        return await EditViewAsync(input, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await gallery.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : await EditViewAsync(model, cancellationToken);
    }

    [HttpPost("{id:int}")]
    public async Task<IActionResult> Edit(int id, AlbumInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        if (ModelState.IsValid)
        {
            var result = await gallery.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage("Album saved.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return await EditViewAsync(input, cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await gallery.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.Title, "album"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await gallery.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Album deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>The album's photos. ?added=12 comes from admin.js after it uploaded photos one by one.</summary>
    [HttpGet("{id:int}/photos")]
    public async Task<IActionResult> Photos(int id, int? added, CancellationToken cancellationToken)
    {
        var model = await gallery.GetPhotosAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        if (added is > 0 and <= 10000)
        {
            SetStatusMessage(AddedMessage(added.Value, waiting: !User.IsInRole(AppRoles.Admin)));
        }

        return View(model);
    }

    /// <summary>
    /// Adds photos. A plain form post (no JavaScript) brings up to 6 and gets a redirect; admin.js sends one photo per
    /// request with "Accept: application/json" and gets a short JSON answer, so any number of photos can be uploaded
    /// without one huge request, and one bad file does not stop the others.
    /// </summary>
    [HttpPost("{id:int}/photos/add")]
    [UploadLimit(images: GalleryAdminService.MaxPhotosPerUpload)]
    public async Task<IActionResult> AddPhotos(int id, List<IFormFile> newPhotos, CancellationToken cancellationToken)
    {
        var actor = new AdminActor(User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole(AppRoles.Admin));
        var outcome = await gallery.AddPhotosAsync(id, newPhotos, actor, cancellationToken);
        var errors = string.Join(" ", outcome.Result.Errors.Select(error => error.Message));

        if (Request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
        {
            return Json(new { added = outcome.Added, error = errors.Length == 0 ? null : errors });
        }

        if (outcome.Result.Succeeded)
        {
            SetStatusMessage(AddedMessage(outcome.Added, outcome.Waiting));
        }
        else
        {
            // Some files may have been added; say so, then list what was refused and why.
            SetErrorMessage(outcome.Added > 0 ? $"{AddedMessage(outcome.Added, outcome.Waiting)} {errors}" : errors);
        }

        return RedirectToAction(nameof(Photos), new { id });
    }

    [HttpPost("{id:int}/photos")]
    public async Task<IActionResult> Photos(int id, AlbumPhotosForm form, CancellationToken cancellationToken)
    {
        var current = await gallery.GetPhotosAsync(id, cancellationToken);
        if (current is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            // Show the typed captions again, with the photos (whose paths and states never come from the form).
            var saved = current.Photos.ToDictionary(photo => photo.Id);
            current.Photos = form.Photos.Where(row => saved.ContainsKey(row.Id)).ToList();
            current.Photos.ForEach(row =>
            {
                row.ImagePath = saved[row.Id].ImagePath;
                row.Status = saved[row.Id].Status;
                row.UploadedBy = saved[row.Id].UploadedBy;
            });
            return View(current);
        }

        await gallery.UpdatePhotosAsync(id, form.Photos, cancellationToken);
        var removed = form.Photos.Count(row => row.Remove);
        SetStatusMessage(removed == 0 ? "Photos saved." : $"Photos saved. {removed} removed for good.");
        return RedirectToAction(nameof(Photos), new { id });
    }

    /// <summary>Photos waiting for an Admin. Editors may look; only Admins decide (the POST below).</summary>
    [HttpGet("review", Name = "admin-gallery-review")]
    public async Task<IActionResult> Review(int? page, CancellationToken cancellationToken) =>
        View(await gallery.GetReviewAsync(page, cancellationToken));

    [HttpPost("review")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Review(PhotoReviewForm form, CancellationToken cancellationToken)
    {
        var approve = form.Decision == "approve";
        if (!approve && form.Decision != "delete")
        {
            return BadRequest();
        }

        if (form.Selected.Count == 0)
        {
            SetErrorMessage("Tick the photos first.");
            return RedirectToAction(nameof(Review));
        }

        var actor = new AdminActor(User.FindFirstValue(ClaimTypes.NameIdentifier)!, IsAdmin: true);
        var changed = await gallery.ReviewAsync(form.Selected, approve, actor, cancellationToken);
        SetStatusMessage(approve
            ? changed == 1 ? "1 photo approved. It is on the website now." : $"{changed} photos approved. They are on the website now."
            : changed == 1 ? "1 photo removed for good." : $"{changed} photos removed for good.");
        return RedirectToAction(nameof(Review));
    }

    private static string AddedMessage(int added, bool waiting)
    {
        var text = added == 1 ? "1 photo added." : $"{added} photos added.";
        return waiting ? text + " An Admin must approve them before they appear on the website." : text;
    }

    private async Task<IActionResult> EditViewAsync(AlbumInputModel model, CancellationToken cancellationToken)
    {
        model.Categories = await gallery.GetCategoriesAsync(cancellationToken);
        model.Events = await gallery.GetEventsAsync(cancellationToken);
        return View("Edit", model);
    }
}
