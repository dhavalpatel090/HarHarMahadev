using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.TagHelpers;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
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
        var (categories, totalPlaces) = await GetCategoriesAsync(cancellationToken);

        return new HomeViewModel
        {
            Site = site,
            Hero = BuildHero(site),
            Stats = BuildStats(site, categories, totalPlaces),
            Facts = BuildFacts(site),
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
    private List<StatViewModel> BuildStats(SiteInfo site, IReadOnlyList<CategoryInfo> categories, int totalPlaces)
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

    private List<VillageFactViewModel> BuildFacts(SiteInfo site)
    {
        var facts = new List<VillageFactViewModel>();
        Add(text["Taluka"], site.Taluka);
        Add(text["District"], site.District);
        Add(text["State"], site.State);
        Add(text["PIN code"], site.PinCode);
        return facts;

        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                facts.Add(new VillageFactViewModel(label, value));
            }
        }
    }

    private async Task<(IReadOnlyList<CategoryInfo> Categories, int TotalPlaces)> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;

        var placeCounts = await db.Places.AsNoTracking()
            .Visible()
            .GroupBy(place => place.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.CategoryId, group => group.Count, cancellationToken);

        var rows = await db.PlaceCategories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .Select(category => new
            {
                category.Id,
                category.Slug,
                Name = gu ? category.NameGu ?? category.Name : category.Name,
                category.IconClass,
                category.MarkerColor,
                category.ShowInStats
            })
            .ToListAsync(cancellationToken);

        var categories = rows
            .Select(row => new CategoryInfo(
                new CategoryTileViewModel
                {
                    Slug = row.Slug,
                    Name = row.Name,
                    Icon = row.IconClass,
                    Color = row.MarkerColor,
                    PlaceCount = placeCounts.GetValueOrDefault(row.Id)
                },
                row.ShowInStats))
            .ToList();

        return (categories, placeCounts.Values.Sum());
    }

    private Task<List<PlaceCardViewModel>> GetFeaturedPlacesAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;
        return db.Places.AsNoTracking()
            .Visible()
            .Where(place => place.IsFeatured)
            .OrderBy(place => place.Category.SortOrder)
            .ThenBy(place => place.Name)
            .Take(4)
            .Select(place => new PlaceCardViewModel
            {
                Slug = place.Slug,
                Name = gu ? place.NameGu ?? place.Name : place.Name,
                Summary = gu ? place.SummaryGu ?? place.Summary : place.Summary,
                CategoryName = gu ? place.Category.NameGu ?? place.Category.Name : place.Category.Name,
                CategoryIcon = place.Category.IconClass,
                CategoryColor = place.Category.MarkerColor,
                ImagePath = place.MainImagePath
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<TimelineItemViewModel>> GetHistoryHighlightsAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;
        var rows = await db.HistoryEntries.AsNoTracking()
            .Visible()
            .Where(entry => entry.IsHighlighted)
            .OrderBy(entry => entry.SortYear)
            .Take(5)
            .Select(entry => new
            {
                YearLabel = gu ? entry.YearLabelGu ?? entry.YearLabel : entry.YearLabel,
                Title = gu ? entry.TitleGu ?? entry.Title : entry.Title,
                Description = gu ? entry.DescriptionGu ?? entry.Description : entry.Description,
                entry.ImagePath
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new TimelineItemViewModel
        {
            YearLabel = row.YearLabel,
            Title = row.Title,
            Description = TextHelpers.Excerpt(row.Description, 140),
            ImagePath = row.ImagePath
        }).ToList();
    }

    private Task<List<ThenNowViewModel>> GetThenNowAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;
        return db.ThenNowComparisons.AsNoTracking()
            .Visible()
            .OrderBy(comparison => comparison.SortOrder)
            .Take(2)
            .Select(comparison => new ThenNowViewModel
            {
                Title = gu ? comparison.TitleGu ?? comparison.Title : comparison.Title,
                Caption = gu ? comparison.CaptionGu ?? comparison.Caption : comparison.Caption,
                ThenLabel = gu ? comparison.ThenLabelGu ?? comparison.ThenLabel : comparison.ThenLabel,
                NowLabel = gu ? comparison.NowLabelGu ?? comparison.NowLabel : comparison.NowLabel,
                ThenImagePath = comparison.ThenImagePath,
                NowImagePath = comparison.NowImagePath
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<EventCardViewModel>> GetUpcomingEventsAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;
        var rows = await db.Events.AsNoTracking()
            .Upcoming(clock.UtcNow)
            .OrderBy(item => item.StartsAt)
            .Take(3)
            .Select(item => new
            {
                item.Slug,
                Title = gu ? item.TitleGu ?? item.Title : item.Title,
                Summary = gu ? item.SummaryGu ?? item.Summary : item.Summary,
                item.StartsAt,
                item.EndsAt,
                item.IsAllDay,
                item.ImagePath,
                Venue = item.Place != null ? (gu ? item.Place.NameGu ?? item.Place.Name : item.Place.Name) : null,
                LocationText = gu ? item.LocationTextGu ?? item.LocationText : item.LocationText
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new EventCardViewModel
        {
            Slug = row.Slug,
            Title = row.Title,
            Summary = row.Summary,
            StartsAt = clock.ToVillageTime(row.StartsAt),
            EndsAt = row.EndsAt is { } end ? clock.ToVillageTime(end) : null,
            IsAllDay = row.IsAllDay,
            Location = row.Venue ?? row.LocationText,
            ImagePath = row.ImagePath
        }).ToList();
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

    private Task<List<AlbumCardViewModel>> GetAlbumsAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;
        return db.GalleryAlbums.AsNoTracking()
            .Visible()
            .OrderByDescending(album => album.CreatedAt)
            .Take(3)
            .Select(album => new AlbumCardViewModel
            {
                Slug = album.Slug,
                Title = gu ? album.TitleGu ?? album.Title : album.Title,
                CategoryName = gu ? album.Category.NameGu ?? album.Category.Name : album.Category.Name,
                TakenOn = album.TakenOn,
                PhotoCount = album.Images.AsQueryable().Count(PublicVisibility.GalleryImageIsVisible),
                CoverImagePath = album.Images.AsQueryable()
                    .Where(PublicVisibility.GalleryImageIsVisible)
                    .OrderBy(image => image.SortOrder)
                    .Select(image => image.ImagePath)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    private Task<List<PersonCardViewModel>> GetFeaturedPeopleAsync(CancellationToken cancellationToken)
    {
        var gu = AppLanguage.IsGujarati;
        return db.People.AsNoTracking()
            .Visible()
            .Where(person => person.IsFeatured)
            .OrderBy(person => person.FullName)
            .Take(4)
            .Select(person => new PersonCardViewModel
            {
                Slug = person.Slug,
                FullName = gu ? person.FullNameGu ?? person.FullName : person.FullName,
                Profession = gu ? person.ProfessionGu ?? person.Profession : person.Profession,
                CategoryName = gu ? person.Category.NameGu ?? person.Category.Name : person.Category.Name,
                CategoryIcon = person.Category.IconClass,
                PhotoPath = person.PhotoPath
            })
            .ToListAsync(cancellationToken);
    }

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
        var gu = AppLanguage.IsGujarati;
        var rows = await db.Places.AsNoTracking()
            .Visible()
            .Where(place => place.Latitude != null && place.Longitude != null)
            .OrderBy(place => place.Name)
            .Select(place => new
            {
                place.Slug,
                Name = gu ? place.NameGu ?? place.Name : place.Name,
                Summary = gu ? place.SummaryGu ?? place.Summary : place.Summary,
                Category = gu ? place.Category.NameGu ?? place.Category.Name : place.Category.Name,
                place.Category.IconClass,
                place.Category.MarkerColor,
                Latitude = place.Latitude!.Value,
                Longitude = place.Longitude!.Value
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0 && !site.HasCoordinates)
        {
            return null;
        }

        return new MapViewModel
        {
            CenterLatitude = site.Latitude,
            CenterLongitude = site.Longitude,
            Zoom = site.MapZoom,
            Markers = rows.Select(row => new MapMarkerViewModel
            {
                Slug = row.Slug,
                Name = row.Name,
                Summary = TextHelpers.Excerpt(row.Summary, 120),
                Category = row.Category,
                Icon = IconNames.ToSymbol(row.IconClass),
                Color = row.MarkerColor,
                Latitude = row.Latitude,
                Longitude = row.Longitude
            }).ToList()
        };
    }

    private sealed record CategoryInfo(CategoryTileViewModel Tile, bool ShowInStats);
}
