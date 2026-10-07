using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Admin;
using Khaparwada.Web.Services.Site;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>Places (public /places and /map) and each place's photos. Admins and Editors.</summary>
[Route("admin/places")]
public class PlacesController(IPlaceAdminService places, ISiteInfoService siteInfo) : AdminControllerBase
{
    /// <summary>/admin/places?category=3&amp;q=lake&amp;page=2. Named so the shared pager can build its links.</summary>
    [HttpGet("", Name = "admin-places")]
    public async Task<IActionResult> Index(int? category, string? q, int? page, CancellationToken cancellationToken) =>
        View(await places.ListAsync(category, q, page, cancellationToken));

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        await EditViewAsync(new PlaceInputModel(), cancellationToken);

    [HttpPost("new")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Create(PlaceInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, "Place added. You can add more photos from its Photos page.", cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await places.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : await EditViewAsync(model, cancellationToken);
    }

    [HttpPost("{id:int}")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Edit(int id, PlaceInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, "Place saved.", cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await places.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.Name, "place"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await places.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Place deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/photos")]
    public async Task<IActionResult> Photos(int id, CancellationToken cancellationToken)
    {
        var model = await places.GetPhotosAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}/photos/add")]
    [UploadLimit(images: PlaceAdminService.MaxPhotosPerUpload)]
    public async Task<IActionResult> AddPhotos(int id, List<IFormFile> newPhotos, CancellationToken cancellationToken)
    {
        var result = await places.AddPhotosAsync(id, newPhotos, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage(newPhotos.Count == 1 ? "Photo added." : "Photos added.");
        }
        else
        {
            // Some files may have been added; list what was refused and why.
            SetErrorMessage(string.Join(" ", result.Errors.Select(error => error.Message)));
        }

        return RedirectToAction(nameof(Photos), new { id });
    }

    [HttpPost("{id:int}/photos")]
    public async Task<IActionResult> Photos(int id, PlacePhotosForm form, CancellationToken cancellationToken)
    {
        var current = await places.GetPhotosAsync(id, cancellationToken);
        if (current is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            // Show the typed captions again, with the photos (whose paths never come from the form).
            var paths = current.Photos.ToDictionary(photo => photo.Id, photo => photo.ImagePath);
            current.Photos = form.Photos.Where(row => paths.ContainsKey(row.Id)).ToList();
            current.Photos.ForEach(row => row.ImagePath = paths[row.Id]);
            return View(current);
        }

        await places.UpdatePhotosAsync(id, form.Photos, cancellationToken);
        SetStatusMessage("Photos saved.");
        return RedirectToAction(nameof(Photos), new { id });
    }

    private async Task<IActionResult> SaveAsync(PlaceInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await places.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage(successMessage);
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        if (input.Id is { } id && await places.GetAsync(id, cancellationToken) is { } saved)
        {
            input.CurrentImageUrl = saved.CurrentImageUrl;
            input.PhotoCount = saved.PhotoCount;
        }

        return await EditViewAsync(input, cancellationToken);
    }

    private async Task<IActionResult> EditViewAsync(PlaceInputModel model, CancellationToken cancellationToken)
    {
        model.Categories = await places.GetCategoriesAsync(cancellationToken);
        // The map picker starts on the village centre once it is known.
        var site = await siteInfo.GetAsync(cancellationToken);
        ViewData["StartLatitude"] = site.Latitude;
        ViewData["StartLongitude"] = site.Longitude;
        ViewData["StartZoom"] = site.MapZoom;
        return View("Edit", model);
    }
}
