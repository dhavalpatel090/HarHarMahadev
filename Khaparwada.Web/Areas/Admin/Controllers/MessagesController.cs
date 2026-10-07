using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>
/// The Contact form's inbox. Admins only: messages carry residents' names, email addresses and phone numbers,
/// and Editors do not need them to write content.
/// </summary>
[Route("admin/messages")]
[Authorize(Policy = Policies.AdminOnly)]
public class MessagesController(IMessageAdminService messages) : AdminControllerBase
{
    /// <summary>/admin/messages (unread), ?show=all, with &amp;page=2.</summary>
    [HttpGet("", Name = "admin-messages")]
    public async Task<IActionResult> Index(MessageListFilter? show, int? page, CancellationToken cancellationToken)
    {
        var filter = show is { } chosen && Enum.IsDefined(chosen) ? chosen : MessageListFilter.Unread;
        return View(await messages.ListAsync(filter, page, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await messages.OpenAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}/unread")]
    public async Task<IActionResult> MarkUnread(int id, CancellationToken cancellationToken)
    {
        if (!await messages.MarkUnreadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        SetStatusMessage("Marked as unread.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!await messages.DeleteAsync(id, cancellationToken))
        {
            return NotFound();
        }

        SetStatusMessage("Message deleted for good.");
        return RedirectToAction(nameof(Index), new { show = "all" });
    }
}
