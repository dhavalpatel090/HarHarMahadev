using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Gallery;

namespace Khaparwada.Web.Services.Gallery;

/// <summary>The album card query, shared by Home, /gallery, an album's "more albums" and an event's page.</summary>
public static class GalleryQueries
{
    /// <summary>Newest first: what was added last is what people come back for.</summary>
    public static IOrderedQueryable<GalleryAlbum> InListOrder(this IQueryable<GalleryAlbum> albums) =>
        albums.OrderByDescending(album => album.CreatedAt).ThenByDescending(album => album.Id);

    /// <summary>Photo count and cover (the first photo by order) count approved photos only.</summary>
    public static IQueryable<AlbumCardViewModel> SelectAlbumCards(this IQueryable<GalleryAlbum> albums, bool gujarati) =>
        albums.Select(album => new AlbumCardViewModel
        {
            Slug = album.Slug,
            Title = gujarati ? album.TitleGu ?? album.Title : album.Title,
            CategoryName = gujarati ? album.Category.NameGu ?? album.Category.Name : album.Category.Name,
            TakenOn = album.TakenOn,
            PhotoCount = album.Images.AsQueryable().Count(PublicVisibility.GalleryImageIsVisible),
            CoverImagePath = album.Images.AsQueryable()
                .Where(PublicVisibility.GalleryImageIsVisible)
                .OrderBy(image => image.SortOrder)
                .ThenBy(image => image.Id)
                .Select(image => image.ImagePath)
                .FirstOrDefault()
        });

    /// <summary>The photos of an album in the order the admin set.</summary>
    public static IOrderedQueryable<GalleryImage> InAlbumOrder(this IQueryable<GalleryImage> images) =>
        images.OrderBy(image => image.SortOrder).ThenBy(image => image.Id);
}
