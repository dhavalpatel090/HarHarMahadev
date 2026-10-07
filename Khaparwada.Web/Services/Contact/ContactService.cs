using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Places;
using Khaparwada.Web.Services.Site;
using Khaparwada.Web.ViewModels.Contact;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Contact;

public interface IContactService
{
    Task<ContactPageViewModel> GetPageAsync(ContactFormModel form, CancellationToken cancellationToken = default);

    /// <summary>Stores the message for the admins' inbox. Returns false for a robot (honeypot filled), which is dropped.</summary>
    Task<bool> SendAsync(ContactFormModel form, CancellationToken cancellationToken = default);
}

public sealed class ContactService(AppDbContext db, ISiteInfoService siteInfo) : IContactService
{
    public async Task<ContactPageViewModel> GetPageAsync(ContactFormModel form, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var site = await siteInfo.GetAsync(cancellationToken);

        // All active links here (the footer shows only the ones marked for it).
        var links = await db.UsefulLinks.AsNoTracking()
            .Where(link => link.IsActive)
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Title)
            .Select(link => new { link.Title, link.TitleGu, link.Url })
            .ToListAsync(cancellationToken);

        return new ContactPageViewModel
        {
            Site = site,
            Map = await db.LoadVillageMapAsync(site, gujarati, cancellationToken),
            UsefulLinks = links.Select(link => new ExternalLink(AppLanguage.Pick(link.Title, link.TitleGu), link.Url)).ToList(),
            Form = form
        };
    }

    public async Task<bool> SendAsync(ContactFormModel form, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(form.Website))
        {
            return false; // the honeypot: only robots fill in the hidden field
        }

        db.ContactMessages.Add(new ContactMessage
        {
            Name = form.Name.Trim(),
            Email = Optional(form.Email),
            Phone = Optional(form.Phone),
            Subject = form.Subject.Trim(),
            Message = form.Message.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? Optional(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
