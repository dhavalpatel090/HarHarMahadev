using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Notices;
using Khaparwada.Web.ViewModels.Notices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Khaparwada.Web.Controllers;

public class NoticesController(INoticeService notices) : Controller
{
    /// <summary>/notices, /notices?category=water, with &amp;page=2.</summary>
    [HttpGet("notices", Name = RouteNames.Notices)]
    public async Task<IActionResult> Index(string? category, int? page, CancellationToken cancellationToken) =>
        View(await notices.GetListAsync(category, page, cancellationToken));

    /// <summary>
    /// A notice on the board. One that has ended answers 410 Gone with a short page instead of its text (an old
    /// WhatsApp link must not pass on outdated information); search engines drop 410 pages.
    /// </summary>
    [HttpGet("notices/{slug}", Name = RouteNames.NoticeDetails)]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var lookup = await notices.GetDetailsAsync(slug, cancellationToken);
        switch (lookup.State)
        {
            case NoticeLookupState.Ended:
                Response.StatusCode = StatusCodes.Status410Gone;
                return View("Ended", lookup);
            case NoticeLookupState.Active when !string.Equals(lookup.Notice!.Slug, slug, StringComparison.Ordinal):
                // SQL Server finds "/notices/Water-Update" too; send it to the one official address.
                return RedirectToRoutePermanent(RouteNames.NoticeDetails, new { slug = lookup.Notice.Slug });
            case NoticeLookupState.Active:
                return View(lookup.Notice);
            default:
                return NotFound();
        }
    }

    /// <summary>The address in the original plan (/Notices/Details/12): a permanent redirect to /notices/{slug}.</summary>
    [HttpGet("notices/details/{id:int}")]
    public async Task<IActionResult> LegacyDetails(int id, CancellationToken cancellationToken)
    {
        var slug = await notices.GetSlugAsync(id, cancellationToken);
        return slug is null ? NotFound() : RedirectToRoutePermanent(RouteNames.NoticeDetails, new { slug });
    }

    /// <summary>
    /// The notice's PDF, only while the notice is on the board. Sent from the private folder with its original
    /// name, "inline" so phones open it in their PDF viewer, and saved under that name.
    /// </summary>
    [HttpGet("notices/{slug}/attachment", Name = RouteNames.NoticeAttachment)]
    public async Task<IActionResult> Attachment(string slug, CancellationToken cancellationToken)
    {
        var file = await notices.GetAttachmentAsync(slug, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(file.FileName);
        Response.Headers.ContentDisposition = disposition.ToString();
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "no-cache"; // checked again each time: a removed notice's file stops at once
        return PhysicalFile(file.FullPath, "application/pdf", enableRangeProcessing: true);
    }
}
