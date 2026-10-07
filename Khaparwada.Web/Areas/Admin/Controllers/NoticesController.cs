using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>The notice board (public /notices, the Home page and the urgent alert bar). Admins and Editors.</summary>
[Route("admin/notices")]
public class NoticesController(INoticeAdminService notices) : AdminControllerBase
{
    /// <summary>/admin/notices (on the board now), ?show=scheduled|ended|hidden|all, with &amp;q=water&amp;page=2.</summary>
    [HttpGet("", Name = "admin-notices")]
    public async Task<IActionResult> Index(NoticeListFilter? show, string? q, int? page, CancellationToken cancellationToken)
    {
        // Enum binding also accepts numbers (?show=7), so anything that is not a real tab shows the board.
        var filter = show is { } chosen && Enum.IsDefined(chosen) ? chosen : NoticeListFilter.Active;
        return View(await notices.ListAsync(filter, q, page, cancellationToken));
    }

    [HttpGet("new")]
    public IActionResult Create() => View("Edit", notices.NewForm());

    [HttpPost("new")]
    [UploadLimit(images: 1, documents: 1)]
    public Task<IActionResult> Create(NoticeInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, "Notice added.", cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await notices.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Edit", model);
    }

    [HttpPost("{id:int}")]
    [UploadLimit(images: 1, documents: 1)]
    public Task<IActionResult> Edit(int id, NoticeInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, "Notice saved.", cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await notices.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View("ConfirmDelete", new ConfirmDeleteViewModel(id, model.Title, "notice"));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await notices.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Notice deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>The attachment as visitors get it, also while the notice is hidden or not yet shown.</summary>
    [HttpGet("{id:int}/attachment")]
    public async Task<IActionResult> Attachment(int id, CancellationToken cancellationToken)
    {
        var file = await notices.GetAttachmentAsync(id, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(file.FileName);
        Response.Headers.ContentDisposition = disposition.ToString();
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(file.FullPath, "application/pdf", enableRangeProcessing: true);
    }

    private async Task<IActionResult> SaveAsync(NoticeInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await notices.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                // Open the tab the notice is on, so a hidden, scheduled or ended notice does not seem to vanish.
                var (tab, where) = input.SavedState switch
                {
                    NoticeState.Hidden => ("hidden", " It is hidden, so it is listed under “Hidden”."),
                    NoticeState.Scheduled => ("scheduled", " It is shown from the date chosen, so it is listed under “Scheduled”."),
                    NoticeState.Ended => ("ended", " Its last day has passed, so it is listed under “Ended”."),
                    _ => ((string?)null, "")
                };
                SetStatusMessage(successMessage + where);
                return RedirectToAction(nameof(Index), new { show = tab });
            }

            AddErrors(result);
        }

        if (input.Id is { } id && await notices.GetAsync(id, cancellationToken) is { } saved)
        {
            input.CurrentImageUrl = saved.CurrentImageUrl;
            input.CurrentAttachmentName = saved.CurrentAttachmentName;
        }

        return View("Edit", input);
    }
}
