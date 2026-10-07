using System.Security.Claims;
using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>
/// Community profiles (public /people) and their review. Admins and Editors write profiles; only Admins approve
/// them (the rules are in <see cref="PersonAdminService"/>).
/// </summary>
[Route("admin/people")]
public class PeopleController(IPersonAdminService people) : AdminControllerBase
{
    /// <summary>/admin/people?status=review&amp;category=3&amp;q=patel&amp;page=2. Named so the shared pager can build its links.</summary>
    [HttpGet("", Name = "admin-people")]
    public async Task<IActionResult> Index(PersonListFilter? status, int? category, string? q, int? page, CancellationToken cancellationToken)
    {
        // Enum binding also accepts numbers (?status=7), so anything that is not a real tab shows them all.
        var filter = status is { } chosen && Enum.IsDefined(chosen) ? chosen : PersonListFilter.All;
        return View(await people.ListAsync(filter, category, q, page, cancellationToken));
    }

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        await EditViewAsync(new PersonInputModel(), cancellationToken);

    [HttpPost("new")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Create(PersonInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await people.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : await EditViewAsync(model, cancellationToken);
    }

    [HttpPost("{id:int}")]
    [UploadLimit(images: 1)]
    public Task<IActionResult> Edit(int id, PersonInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await people.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.FullName, "profile"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await people.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Profile deleted. It is no longer on the website.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(PersonInputModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            // The role comes from the sign-in cookie, never from the form: an Editor cannot post a decision.
            var actor = new AdminActor(User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole(AppRoles.Admin));
            var outcome = await people.SaveAsync(input, actor, cancellationToken);
            if (outcome.Result.Succeeded)
            {
                SetStatusMessage(SavedMessage(outcome));
                return RedirectToAction(nameof(Index));
            }

            AddErrors(outcome.Result);
        }

        if (input.Id is { } id && await people.GetAsync(id, cancellationToken) is { } saved)
        {
            // Shown again with what is stored now (the photo, the review history), never with what was posted.
            input.CurrentPhotoUrl = saved.CurrentPhotoUrl;
            input.CurrentStatus = saved.CurrentStatus;
            input.ConsentRecordedAt = saved.ConsentRecordedAt;
            input.ReviewedBy = saved.ReviewedBy;
            input.ReviewedAt = saved.ReviewedAt;
        }

        return await EditViewAsync(input, cancellationToken);
    }

    /// <summary>Says plainly whether the profile is on the website now, and if not, why.</summary>
    private static string SavedMessage(PersonSaveOutcome outcome) => outcome.After switch
    {
        ModerationStatus.Published => "Profile saved. It is shown on the website.",
        ModerationStatus.Approved => "Profile saved. It is approved but not shown on the website.",
        ModerationStatus.Rejected => "Profile saved and sent back. It is not on the website.",
        _ when outcome.LeftWebsite => "Changes saved and sent for review: an Admin must approve them, so the profile is hidden from the website until then.",
        _ => "Profile saved and sent for review: an Admin must approve it before it appears on the website."
    };

    private async Task<IActionResult> EditViewAsync(PersonInputModel model, CancellationToken cancellationToken)
    {
        model.Categories = await people.GetCategoriesAsync(cancellationToken);
        return View("Edit", model);
    }
}
