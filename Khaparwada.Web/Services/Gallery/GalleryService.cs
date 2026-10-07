using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Gallery;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Gallery;

public interface IGalleryService
{
    Task<GalleryListViewModel> GetListAsync(string? category, int? page, CancellationToken cancellationToken = default);
    Task<AlbumDetailsViewModel?> GetAlbumAsync(string slug, int? page, CancellationToken cancellationToken = default);
}

/// <summary>The public gallery. Every query goes through PublicVisibility (approved photos, published albums).</summary>
public sealed class GalleryService(AppDbContext db, IVillageClock clock) : IGalleryService
{
    public const int AlbumsPerPage = 12;

    /// <summary>Photos per album page: the rest load on the next page, never all at once.</summary>
    public const int PhotosPerPage = 30;

    public async Task<GalleryListViewModel> GetListAsync(string? category, int? page, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var categories = await LoadCategoriesAsync(gujarati, cancellationToken);
        var selected = categories.FirstOrDefault(c => string.Equals(c.Slug, category, StringComparison.OrdinalIgnoreCase));

        var query = db.GalleryAlbums.AsNoTracking().Visible();
        if (selected is not null)
        {
            query = query.Where(album => album.Category.Slug == selected.Slug);
        }

        var total = await query.CountAsync(cancellationToken);
        var currentPage = PaginationViewModel.Clamp(page, total, AlbumsPerPage);
        var albums = await query
            .InListOrder()
            .Skip((currentPage - 1) * AlbumsPerPage)
            .Take(AlbumsPerPage)
            .SelectAlbumCards(gujarati)
            .ToListAsync(cancellationToken);

        return new GalleryListViewModel
        {
            Categories = categories,
            Category = selected,
            Albums = albums,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = AlbumsPerPage,
                TotalItems = total,
                RouteName = RouteNames.Gallery,
                RouteValues = new Dictionary<string, string?> { ["category"] = selected?.Slug }
            }
        };
    }

    public async Task<AlbumDetailsViewModel?> GetAlbumAsync(string slug, int? page, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var album = await db.GalleryAlbums.AsNoTracking()
            .Visible()
            .Where(a => a.Slug == slug)
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.CategoryId,
                Title = gujarati ? a.TitleGu ?? a.Title : a.Title,
                Description = gujarati ? a.DescriptionGu ?? a.Description : a.Description,
                CategorySlug = a.Category.Slug,
                CategoryName = gujarati ? a.Category.NameGu ?? a.Category.Name : a.Category.Name,
                a.TakenOn,
                EventSlug = a.Event != null && a.Event.IsPublished ? a.Event.Slug : null,
                EventTitle = a.Event != null && a.Event.IsPublished ? gujarati ? a.Event.TitleGu ?? a.Event.Title : a.Event.Title : null
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (album is null)
        {
            return null;
        }

        var photos = db.GalleryImages.AsNoTracking()
            .Where(image => image.AlbumId == album.Id)
            .Where(PublicVisibility.GalleryImageIsVisible);
        var total = await photos.CountAsync(cancellationToken);
        var currentPage = PaginationViewModel.Clamp(page, total, PhotosPerPage);
        var rows = await photos
            .InAlbumOrder()
            .Skip((currentPage - 1) * PhotosPerPage)
            .Take(PhotosPerPage)
            .Select(image => new
            {
                image.ImagePath,
                image.Width,
                image.Height,
                Caption = gujarati ? image.CaptionGu ?? image.Caption : image.Caption,
                image.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var moreAlbums = await db.GalleryAlbums.AsNoTracking()
            .Visible()
            .Where(a => a.CategoryId == album.CategoryId && a.Id != album.Id)
            .InListOrder()
            .Take(3)
            .SelectAlbumCards(gujarati)
            .ToListAsync(cancellationToken);

        var category = new GalleryCategoryViewModel(album.CategorySlug, album.CategoryName, 0);
        return new AlbumDetailsViewModel
        {
            Slug = album.Slug,
            Title = album.Title,
            Description = album.Description,
            Category = category,
            TakenOn = album.TakenOn,
            EventSlug = album.EventSlug,
            EventTitle = album.EventTitle,
            Photos = rows.Select(row => new GalleryPhotoViewModel
            {
                ImagePath = row.ImagePath,
                Width = row.Width,
                Height = row.Height,
                Caption = row.Caption,
                AddedOn = DateOnly.FromDateTime(clock.ToVillageTime(row.CreatedAt).DateTime)
            }).ToList(),
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PhotosPerPage,
                TotalItems = total,
                RouteName = RouteNames.AlbumDetails,
                RouteValues = new Dictionary<string, string?> { ["slug"] = album.Slug }
            },
            MoreAlbums = moreAlbums
        };
    }

    /// <summary>Switched-on categories with their number of visible albums (chips only for those with albums).</summary>
    private async Task<List<GalleryCategoryViewModel>> LoadCategoriesAsync(bool gujarati, CancellationToken cancellationToken)
    {
        var counts = await db.GalleryAlbums.AsNoTracking()
            .Visible()
            .GroupBy(album => album.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.CategoryId, group => group.Count, cancellationToken);

        var rows = await db.GalleryCategories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new
            {
                category.Id,
                category.Slug,
                Name = gujarati ? category.NameGu ?? category.Name : category.Name
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new GalleryCategoryViewModel(row.Slug, row.Name, counts.GetValueOrDefault(row.Id))).ToList();
    }
}
