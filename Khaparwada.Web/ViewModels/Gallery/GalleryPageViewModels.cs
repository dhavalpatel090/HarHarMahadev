using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.Gallery;

/// <summary>A category chip on /gallery; Count = albums visitors can open.</summary>
public sealed record GalleryCategoryViewModel(string Slug, string Name, int Count);

/// <summary>/gallery: albums, newest first, optionally of one category.</summary>
public sealed class GalleryListViewModel
{
    public required IReadOnlyList<GalleryCategoryViewModel> Categories { get; init; }
    public GalleryCategoryViewModel? Category { get; init; }
    public required IReadOnlyList<AlbumCardViewModel> Albums { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>One photo of an album page.</summary>
public sealed class GalleryPhotoViewModel
{
    public required string ImagePath { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string? Caption { get; init; }

    /// <summary>The upload date in the village's time zone.</summary>
    public DateOnly AddedOn { get; init; }

    public string? MediumUrl => MediaUrls.Image(ImagePath, ImageSize.Medium);
    public string? LargeUrl => MediaUrls.Image(ImagePath, ImageSize.Large);
    public string? SrcSet => MediaUrls.SrcSet(ImagePath);
}

/// <summary>/gallery/{slug}: one page of an album's approved photos.</summary>
public sealed class AlbumDetailsViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required GalleryCategoryViewModel Category { get; init; }
    public DateOnly? TakenOn { get; init; }

    /// <summary>The event these photos are from, while its page is public.</summary>
    public string? EventSlug { get; init; }
    public string? EventTitle { get; init; }

    public required IReadOnlyList<GalleryPhotoViewModel> Photos { get; init; }
    public required PaginationViewModel Pagination { get; init; }
    public required IReadOnlyList<AlbumCardViewModel> MoreAlbums { get; init; }
}
