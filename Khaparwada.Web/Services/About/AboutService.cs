using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.History;
using Khaparwada.Web.Services.Site;
using Khaparwada.Web.ViewModels.About;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Khaparwada.Web.Services.About;

public interface IAboutService
{
    Task<AboutPageViewModel> GetPageAsync(CancellationToken cancellationToken = default);
}

/// <summary>The About page: village facts, the admin-written sections and the timeline's milestones.</summary>
public sealed class AboutService(
    AppDbContext db,
    ISiteInfoService siteInfo,
    IStringLocalizer<SharedResource> text) : IAboutService
{
    public async Task<AboutPageViewModel> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var site = await siteInfo.GetAsync(cancellationToken);

        var sections = await db.AboutSections.AsNoTracking()
            .Visible()
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Id)
            .Select(section => new AboutSectionViewModel
            {
                Anchor = section.Slug,
                Title = gujarati ? section.TitleGu ?? section.Title : section.Title,
                Body = gujarati ? section.BodyGu ?? section.Body : section.Body,
                ImagePath = section.ImagePath
            })
            .ToListAsync(cancellationToken);

        var milestones = await db.HistoryEntries.AsNoTracking()
            .Visible()
            .Where(entry => entry.IsHighlighted)
            .InTimelineOrder()
            .SelectTimelineItems(gujarati)
            .ToListAsync(cancellationToken);

        return new AboutPageViewModel
        {
            Site = site,
            Facts = [.. VillageFacts.Location(site, text), .. VillageFacts.Figures(site, text)],
            Sections = sections,
            Milestones = milestones
                .Select(item => item with { Description = TextHelpers.Excerpt(item.Description, 180) })
                .ToList()
        };
    }
}
