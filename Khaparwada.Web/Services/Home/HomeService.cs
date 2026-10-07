using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.TagHelpers;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Events;
using Khaparwada.Web.Services.Gallery;
using Khaparwada.Web.Services.History;
using Khaparwada.Web.Services.People;
using Khaparwada.Web.Services.Places;
using Khaparwada.Web.Services.Site;
using Khaparwada.Web.ViewModels.Events;
using Khaparwada.Web.ViewModels.Gallery;
using Khaparwada.Web.ViewModels.History;
using Khaparwada.Web.ViewModels.Home;
using Khaparwada.Web.ViewModels.Map;
using Khaparwada.Web.ViewModels.Memories;
using Khaparwada.Web.ViewModels.Notices;
using Khaparwada.Web.ViewModels.People;
using Khaparwada.Web.ViewModels.Places;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Khaparwada.Web.Services.Home;

public interface IHomeService
{
    Task<HomeViewModel> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds the Home page: one small query per section, each selecting only the columns it shows, and all of
/// them filtered by <see cref="PublicVisibility"/>. Text columns are picked as "Gujarati, else English" in
/// SQL, so only one language travels from the database.
/// </summary>
public sealed class HomeService(
    AppDbContext db,
    ISiteInfoService siteInfo,
    IVillageClock clock,
    IStringLocalizer<SharedResource> text) : IHomeService
{
    public async Task<HomeViewModel> GetAsync(CancellationToken cancellationToken = default)
    {
        var site = await siteInfo.GetAsync(cancellationToken);
        var categories = await db.LoadCategoriesAsync(AppLanguage.IsGujarati, cancellationToken);
        var totalPlaces = categories.Sum(category => category.Tile.PlaceCount);

        return new HomeViewModel
        {
            Site = site,
            Hero = BuildHero(site),
            Stats = BuildStats(site, categories, totalPlaces),
            Facts = VillageFacts.Location(site, text),
            PlaceCategories = categories.Select(category => category.Tile).ToList(),
            FeaturedPlaces = await GetFeaturedPlacesAsync(cancellationToken),
            HistoryHighlights = await GetHistoryHighlightsAsync(cancellationToken),
            ThenNow = await GetThenNowAsync(cancellationToken),
            UpcomingEvents = await GetUpcomingEventsAsync(cancellationToken),
            LatestNotices = await GetLatestNoticesAsync(cancellationToken),
            Albums = await GetAlbumsAsync(cancellationToken),
            FeaturedPeople = await GetFeaturedPeopleAsync(cancellationToken),
            Memories = await GetMemoriesAsync(cancellationToken),
            Map = await GetMapAsync(site, cancellationToken)
        };
    }

    private HeroViewModel BuildHero(SiteInfo site) => new()
    {
        Title = text["Welcome to {0}", site.VillageName],
        Tagline = site.Tagline,
        VillageName = site.VillageName,
        ImageUrl = site.HeroImageUrl ?? MediaUrls.VillageIllustration,
        IsIllustration = site.HeroImageUrl is null,
        VideoUrl = site.HeroVideoUrl
    };

    /// <summary>
    /// Typed-in figures (population, families, area) appear only once the admin has entered them; the
    /// counts of schools, temples and places are always computed from the Places table.
    /// </summary>
    private List<StatViewModel> BuildStats(SiteInfo site, IReadOnlyList<PlaceCategoryCount> categories, int totalPlaces)
    {
        var stats = new List<StatViewModel>();
        if (site.Population is { } population)
        {
            stats.Add(new StatViewModel("people-group", population.ToString("N0"), text["Population"]));
        }

        if (site.Households is { } households)
        {
            stats.Add(new StatViewModel("people-roof", households.ToString("N0"), text["Families"]));
        }

        if (site.AreaSqKm is { } area)
        {
            stats.Add(new StatViewModel("ruler-combined", text["{0} km²", area.ToString("0.##")], text["Area"]));
        }

        stats.AddRange(categories
            .Where(category => category.ShowInStats)
            .Select(category => new StatViewModel(category.Tile.Icon, category.Tile.PlaceCount.ToString("N0"), category.Tile.Name)));

        stats.Add(new StatViewModel("location-dot", totalPlaces.ToString("N0"), text["Important places"]));
        return stats;
    }

    private Task<List<PlaceCardViewModel>> GetFeaturedPlacesAsync(CancellationToken cancellationToken) =>
        db.Places.AsNoTracking()
            .Visible()
            .Where(place => place.IsFeatured)
            .InListOrder()
            .Take(4)
            .SelectCards(AppLanguage.IsGujarati)
            .ToListAsync(cancellationToken);

    private async Task<List<TimelineItemViewModel>> GetHistoryHighlightsAsync(CancellationToken cancellationToken)
    {
        var items = await db.HistoryEntries.AsNoTracking()
            .Visible()
            .Where(entry => entry.IsHighlighted)
            .InTimelineOrder()
            .Take(5)
            .SelectTimelineItems(AppLanguage.IsGujarati)
            .ToListAsync(cancellationToken);

        return items.Select(item => item with { Description = TextHelpers.Excerpt(item.Description, 140) }).ToList();
    }

    private Task<List<ThenNowViewModel>> GetThenNowAsync(CancellationToken cancellationToken) =>
        db.ThenNowComparisons.AsNoTracking()
            .Visible()
            .InDisplayOrder()
            .Take(2)
            .SelectThenNow(AppLanguage.IsGujarati)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The next three events. Featured events always get a place while they are upcoming (the admin's way of
    /// saying "don't miss this"); the cards are then shown in date order.
    /// </summary>
    private async Task<List<EventCardViewModel>> GetUpcomingEventsAsync(CancellationToken cancellationToken)
    {
        var cards = await db.Events.AsNoTracking()
            .Upcoming(clock.UtcNow)
            .OrderByDescending(item => item.IsFeatured)
            .ThenBy(item => item.StartsAt)
            .Take(3)
            .SelectCardRows(AppLanguage.IsGujarati)
            .ToCardsAsync(clock, cancellationToken);
        return cards.OrderBy(card => card.StartsAt).ToList();
    }

    private async Task<List<NoticeItemViewModel>> GetLatestNoticesAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;
        var rows = await db.Notices.AsNoTracking()
            .Active(clock.UtcNow)
            // Priority is stored as text, so ORDER BY Priority would sort alphabetically; rank it explicitly.
            .OrderByDescending(notice => notice.Priority == NoticePriority.Urgent)
            .ThenByDescending(notice => notice.Priority == NoticePriority.Important)
            .ThenByDescending(notice => notice.PublishedAt)
            .Take(4)
            .Select(notice => new
            {
                notice.Slug,
                Title = gu ? notice.TitleGu ?? notice.Title : notice.Title,
                notice.Category,
                notice.Priority,
                notice.PublishedAt
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new NoticeItemViewModel
        {
            Slug = row.Slug,
            Title = row.Title,
            Category = row.Category,
            Priority = row.Priority,
            PublishedAt = clock.ToVillageTime(row.PublishedAt)
        }).ToList();
    }

    private Task<List<AlbumCardViewModel>> GetAlbumsAsync(CancellationToken cancellationToken) =>
        db.GalleryAlbums.AsNoTracking()
            .Visible()
            .InListOrder()
            .Take(3)
            .SelectAlbumCards(AppLanguage.IsGujarati)
            .ToListAsync(cancellationToken);

    private Task<List<PersonCardViewModel>> GetFeaturedPeopleAsync(CancellationToken cancellationToken) =>
        db.People.AsNoTracking()
            .Visible()
            .Where(person => person.IsFeatured)
            .OrderBy(person => person.FullName)
            .Take(4)
            .SelectCards(AppLanguage.IsGujarati)
            .ToListAsync(cancellationToken);

    private async Task<List<MemoryCardViewModel>> GetMemoriesAsync(CancellationToken cancellationToken)
    {
        var rows = await db.MemorySubmissions.AsNoTracking()
            .Visible()
            .Where(memory => memory.Slug != null)
            .OrderByDescending(memory => memory.PublishedAt)
            .Take(3)
            .Select(memory => new { memory.Slug, memory.Title, memory.Story, memory.EraLabel, memory.SubmitterName })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new MemoryCardViewModel
        {
            Slug = row.Slug!,
            Title = row.Title,
            Excerpt = TextHelpers.Excerpt(row.Story, 160),
            EraLabel = row.EraLabel,
            SubmitterName = row.SubmitterName
        }).ToList();
    }

    private async Task<MapViewModel?> GetMapAsync(SiteInfo site, CancellationToken cancellationToken)
    {
        var markers = await db.Places.AsNoTracking()
            .Visible()
            .InListOrder()
            .SelectMarkers(AppLanguage.IsGujarati)
            .ToListAsync(cancellationToken);

        if (markers.Count == 0 && !site.HasCoordinates)
        {
            return null;
        }

        return new MapViewModel
        {
            CenterLatitude = site.Latitude,
            CenterLongitude = site.Longitude,
            Zoom = site.MapZoom,
            Markers = markers
        };
    }
}
