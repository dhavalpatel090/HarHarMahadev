using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Services.Contact;
using Khaparwada.Web.ViewModels.Contact;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Khaparwada.Web.Controllers;

public class ContactController(IContactService contact, IStringLocalizer<SharedResource> localizer) : Controller
{
    /// <summary>/contact, or /contact?subject=… from a link that already knows the topic (e.g. a profile's page).</summary>
    [HttpGet("contact", Name = RouteNames.Contact)]
    public async Task<IActionResult> Index(string? subject, CancellationToken cancellationToken)
    {
        var form = new ContactFormModel();
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var text = subject.Trim();
            form.Subject = text.Length <= FieldLengths.Title ? text : text[..FieldLengths.Title];
        }

        return View(await contact.GetPageAsync(form, cancellationToken));
    }

    /// <summary>
    /// Spam protection without tracking visitors: the antiforgery token (global), the hidden honeypot field and
    /// at most 5 messages per network address every 15 minutes (a 429 page explains the wait).
    /// </summary>
    [HttpPost("contact")]
    [EnableRateLimiting(Policies.ContactRateLimit)]
    public async Task<IActionResult> Index([Bind(Prefix = "Form")] ContactFormModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Email) && string.IsNullOrWhiteSpace(form.Phone))
        {
            ModelState.AddModelError("Form.Email", localizer["Please give an email address or a phone number, so the office can reply."]);
        }

        if (!ModelState.IsValid)
        {
            return View(await contact.GetPageAsync(form, cancellationToken));
        }

        // A robot's message (honeypot) gets the same thank-you page and is dropped: it learns nothing.
        await contact.SendAsync(form, cancellationToken);
        return RedirectToRoute(RouteNames.ContactSent);
    }

    /// <summary>After sending (Post/Redirect/Get: reloading this page cannot send the message twice).</summary>
    [HttpGet("contact/sent", Name = RouteNames.ContactSent)]
    public IActionResult Sent() => View();
}
