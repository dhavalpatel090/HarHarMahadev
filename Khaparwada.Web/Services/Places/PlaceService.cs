using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Infrastructure.TagHelpers;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Site;
using Khaparwada.Web.ViewModels.Events;
using Khaparwada.Web.ViewModels.Map;
using Khaparwada.Web.ViewModels.Places;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Places;

public interface IPlaceService
{
    /// <summary>One page of places, optionally of one category and/or matching a search.</summary>
    Task<PlaceListViewModel> GetListAsync(string? category, string? search, int? page, CancellationToken cancellationToken = default);

    Task<PlaceDetailsViewModel?> GetDetailsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>The web address of a visible place, for old /Places/Details/{id} links.</summary>
    Task<string?> GetSlugAsync(int id, CancellationToken cancellationToken = default);

    Task<MapPageViewModel> GetMapPageAsync(string? category, CancellationToken cancellationToken = default);
}

/// <summary>The public Places pages and the village map. Every query goes through PublicVisibility.</summary>
public sealed class PlaceService(AppDbContext db, ISiteInfoService siteInfo, IVillageClock clock) : IPlaceService
{
    public const int PageSize = 12;
    public const int MaxSearchLength = 100;

    public async Task<PlaceListViewModel> GetListAsync(string? category, string? search, int? page, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var categories = await db.LoadCategoriesAsync(gujarati, cancellationToken);
        var selected = categories.FirstOrDefault(c => string.Equals(c.Tile.Slug, category, StringComparison.OrdinalIgnoreCase));
        search = NormalizeSearch(search);

        var query = db.Places.AsNoTracking().Visible();
        if (selected is not null)
        {
            query = query.Where(place => place.CategoryId == selected.Id);
        }

        if (search is not null)
        {
            // EF passes the text as a parameter and escapes LIKE wildcards, so % or _ in a search are plain characters.
            query = query.Where(place =>
                place.Name.Contains(search) || (place.NameGu != null && place.NameGu.Contains(search)) ||
                place.Summary.Contains(search) || (place.SummaryGu != null && place.SummaryGu.Contains(search)) ||
                (place.Address != null && place.Address.Contains(search)) || (place.AddressGu != null && place.AddressGu.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var places = await query
            .InListOrder()
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .SelectCards(gujarati)
            .ToListAsync(cancellationToken);

        return new PlaceListViewModel
        {
            Categories = categories.Select(c => c.Tile).ToList(),
            Category = selected?.Tile,
            Search = search,
            Places = places,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = RouteNames.Places,
                RouteValues = new Dictionary<string, string?> { ["category"] = selected?.Tile.Slug, ["q"] = search }
            }
        };
    }

    public async Task<PlaceDetailsViewModel?> GetDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var place = await db.Places.AsNoTracking()
            .Visible()
            .Where(p => p.Slug == slug)
            .Select(p => new
            {
                p.Id,
                p.Slug,
                p.CategoryId,
                Name = gujarati ? p.NameGu ?? p.Name : p.Name,
                Summary = gujarati ? p.SummaryGu ?? p.Summary : p.Summary,
                Description = gujarati ? p.DescriptionGu ?? p.Description : p.Description,
                Address = gujarati ? p.AddressGu ?? p.Address : p.Address,
                Timings = gujarati ? p.TimingsGu ?? p.Timings : p.Timings,
                p.Phone,
                p.Email,
                p.Website,
                p.Latitude,
                p.Longitude,
                p.MainImagePath,
                CategorySlug = p.Category.Slug,
                CategoryName = gujarati ? p.Category.NameGu ?? p.Category.Name : p.Category.Name,
                p.Category.IconClass,
                p.Category.MarkerColor
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (place is null)
        {
            return null;
        }

        var photos = await db.PlaceImages.AsNoTracking()
            .Where(image => image.PlaceId == place.Id)
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .Select(image => new PlacePhotoViewModel(
                image.ImagePath, image.Width, image.Height, gujarati ? image.CaptionGu ?? image.Caption : image.Caption))
            .ToListAsync(cancellationToken);

        var events = await db.Events.AsNoTracking()
            .Upcoming(clock.UtcNow)
            .Where(item => item.PlaceId == place.Id)
            .OrderBy(item => item.StartsAt)
            .Take(3)
            .Select(item => new
            {
                item.Slug,
                Title = gujarati ? item.TitleGu ?? item.Title : item.Title,
                Summary = gujarati ? item.SummaryGu ?? item.Summary : item.Summary,
                item.StartsAt,
                item.EndsAt,
                item.IsAllDay,
                item.ImagePath
            })
            .ToListAsync(cancellationToken);

        var morePlaces = await db.Places.AsNoTracking()
            .Visible()
            .Where(p => p.CategoryId == place.CategoryId && p.Id != place.Id)
            .InListOrder()
            .Take(3)
            .SelectCards(gujarati)
            .ToListAsync(cancellationToken);

        var category = new CategoryTileViewModel
        {
            Slug = place.CategorySlug,
            Name = place.CategoryName,
            Icon = place.IconClass,
            Color = place.MarkerColor
        };

        return new PlaceDetailsViewModel
        {
            Slug = place.Slug,
            Name = place.Name,
            Summary = place.Summary,
            Description = place.Description,
            Category = category,
            Address = place.Address,
            Timings = place.Timings,
            Phone = place.Phone,
            Email = place.Email,
            Website = place.Website,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            MainImagePath = place.MainImagePath,
            Photos = photos,
            UpcomingEvents = events.Select(item => new EventCardViewModel
            {
                Slug = item.Slug,
                Title = item.Title,
                Summary = item.Summary,
                StartsAt = clock.ToVillageTime(item.StartsAt),
                EndsAt = item.EndsAt is { } end ? clock.ToVillageTime(end) : null,
                IsAllDay = item.IsAllDay,
                Location = place.Name,
                ImagePath = item.ImagePath
            }).ToList(),
            MorePlaces = morePlaces,
            Map = place.Latitude is { } latitude && place.Longitude is { } longitude
                ? new MapViewModel
                {
                    CenterLatitude = latitude,
                    CenterLongitude = longitude,
                    Zoom = 16,
                    Markers =
                    [
                        new MapMarkerViewModel
                        {
                            Slug = place.Slug,
                            Name = place.Name,
                            Summary = place.Summary,
                            Category = place.CategoryName,
                            CategorySlug = place.CategorySlug,
                            Icon = place.IconClass,
                            Color = place.MarkerColor,
                            ImagePath = place.MainImagePath,
                            Latitude = latitude,
                            Longitude = longitude
                        }
                    ]
                }
                : null
        };
    }

    public Task<string?> GetSlugAsync(int id, CancellationToken cancellationToken = default) =>
        db.Places.AsNoTracking()
            .Visible()
            .Where(place => place.Id == id)
            .Select(place => place.Slug)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<MapPageViewModel> GetMapPageAsync(string? category, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var site = await siteInfo.GetAsync(cancellationToken);
        var categories = await db.LoadCategoriesAsync(gujarati, cancellationToken);
        var markers = await db.Places.AsNoTracking()
            .Visible()
            .InListOrder()
            .SelectMarkers(gujarati)
            .ToListAsync(cancellationToken);

        var groups = categories
            .Select(c => new MapCategoryViewModel(
                c.Tile.Slug, c.Tile.Name, IconNames.ToSymbol(c.Tile.Icon), c.Tile.Color,
                markers.Where(marker => marker.CategorySlug == c.Tile.Slug).ToList()))
            .Where(group => group.Places.Count > 0)
            .ToList();

        return new MapPageViewModel
        {
            Map = new MapViewModel
            {
                CenterLatitude = site.Latitude,
                CenterLongitude = site.Longitude,
                Zoom = site.MapZoom,
                Markers = markers
            },
            Categories = groups,
            SelectedCategory = groups.FirstOrDefault(g => string.Equals(g.Slug, category, StringComparison.OrdinalIgnoreCase))?.Slug
        };
    }

    /// <summary>Trimmed, shortened to a sane length, inner spaces collapsed; null when nothing is left.</summary>
    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var text = string.Join(' ', search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length <= MaxSearchLength ? text : text[..MaxSearchLength];
    }
}
