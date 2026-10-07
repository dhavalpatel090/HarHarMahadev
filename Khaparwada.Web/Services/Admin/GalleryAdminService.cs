using System.Globalization;
using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

/// <summary>The result of an upload: how many photos were added, and whether they wait for an Admin.</summary>
public sealed record PhotoUploadOutcome(OperationResult Result, int Added, bool Waiting);

public interface IGalleryAdminService
{
    Task<AlbumAdminListViewModel> ListAsync(int? categoryId, string? search, int? page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlbumEventOption>> GetEventsAsync(CancellationToken cancellationToken = default);
    Task<AlbumInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the album when input.Id is null, otherwise updates it.</summary>
    Task<OperationResult> SaveAsync(AlbumInputModel input, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the album and its photos leave the website at once; all stay recoverable.</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<AlbumPhotosForm?> GetPhotosAsync(int albumId, CancellationToken cancellationToken = default);

    /// <summary>Adds photos: shown at once when an Admin uploads them, otherwise waiting for an Admin's approval.</summary>
    Task<PhotoUploadOutcome> AddPhotosAsync(int albumId, IReadOnlyList<IFormFile> files, AdminActor actor, CancellationToken cancellationToken = default);

    /// <summary>Saves captions and order, and permanently removes the ticked photos (rows and files).</summary>
    Task<OperationResult> UpdatePhotosAsync(int albumId, IReadOnlyList<AlbumPhotoRow> rows, CancellationToken cancellationToken = default);

    Task<PhotoReviewViewModel> GetReviewAsync(int? page, CancellationToken cancellationToken = default);

    /// <summary>Approves the given waiting photos, or removes them permanently. Returns how many were changed.</summary>
    Task<int> ReviewAsync(IReadOnlyList<int> photoIds, bool approve, AdminActor actor, CancellationToken cancellationToken = default);
}

/// <summary>
/// Albums and their photos. Review rule (as for people): photos uploaded by an Admin are shown at once; an
/// Editor's photos wait until an Admin approves them. Removing a photo is permanent, files included, unlike other
/// content: a photo taken down because someone asked must not stay reachable at its old address.
/// </summary>
public sealed class GalleryAdminService(AppDbContext db, IImageUploadService images, IVillageClock clock) : IGalleryAdminService
{
    public const int PageSize = 25;
    public const int ReviewPageSize = 48;

    /// <summary>Per request. The Photos page sends one photo per request when JavaScript runs, so any number fits.</summary>
    public const int MaxPhotosPerUpload = 6;

    private const string MediaFolder = "gallery";
    private const string ListRouteName = "admin-gallery";
    private const string ReviewRouteName = "admin-gallery-review";

    public async Task<AlbumAdminListViewModel> ListAsync(int? categoryId, string? search, int? page, CancellationToken cancellationToken = default)
    {
        search = AdminText.Optional(search);
        var query = db.GalleryAlbums.AsNoTracking();
        if (categoryId is { } id)
        {
            query = query.Where(album => album.CategoryId == id);
        }

        if (search is not null)
        {
            query = query.Where(album => album.Title.Contains(search) || (album.TitleGu != null && album.TitleGu.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var items = await query
            .OrderByDescending(album => album.CreatedAt)
            .ThenByDescending(album => album.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(album => new AlbumListItem(
                album.Id, album.Title, album.TitleGu, album.Category.Name, album.Category.IsActive, album.TakenOn,
                album.Images.OrderBy(image => image.SortOrder).ThenBy(image => image.Id).Select(image => image.ImagePath).FirstOrDefault(),
                album.Images.Count(image => image.Status == ModerationStatus.Published),
                album.Images.Count(image => image.Status == ModerationStatus.Pending || image.Status == ModerationStatus.UnderReview),
                album.IsPublished,
                album.Title.StartsWith("[")))
            .ToListAsync(cancellationToken);

        return new AlbumAdminListViewModel
        {
            Items = items,
            Categories = await GetCategoriesAsync(cancellationToken),
            CategoryId = categoryId,
            Search = search,
            WaitingTotal = await CountWaitingAsync(cancellationToken),
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = ListRouteName,
                RouteValues = new Dictionary<string, string?> { ["category"] = categoryId?.ToString(), ["q"] = search }
            }
        };
    }

    public async Task<IReadOnlyList<PlaceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await db.GalleryCategories.AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new PlaceCategoryOption(category.Id, category.Name, category.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AlbumEventOption>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Events.AsNoTracking()
            .OrderByDescending(item => item.StartsAt)
            .Select(item => new { item.Id, item.Title, item.StartsAt })
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new AlbumEventOption(row.Id, $"{row.Title}, {clock.ToVillageTime(row.StartsAt).ToString("d MMM yyyy", CultureInfo.CurrentCulture)}"))
            .ToList();
    }

    public async Task<AlbumInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var album = await db.GalleryAlbums.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        return album is null
            ? null
            : new AlbumInputModel
            {
                Id = album.Id,
                CategoryId = album.CategoryId,
                Title = album.Title,
                TitleGu = album.TitleGu,
                Slug = album.Slug,
                Description = album.Description,
                DescriptionGu = album.DescriptionGu,
                TakenOn = album.TakenOn,
                EventId = album.EventId,
                IsPublished = album.IsPublished
            };
    }

    public async Task<OperationResult> SaveAsync(AlbumInputModel input, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Create(input.Title) : input.Slug.Trim();
        if (!SlugGenerator.IsValid(slug))
        {
            return OperationResult.Failure(nameof(input.Slug), "Enter a web address in English letters, e.g. navratri-2026.");
        }

        if (await db.GalleryAlbums.AnyAsync(a => a.Slug == slug && a.Id != input.Id, cancellationToken))
        {
            return OperationResult.Failure(nameof(input.Slug), "Another album already uses this web address.");
        }

        if (!await db.GalleryCategories.AnyAsync(c => c.Id == input.CategoryId, cancellationToken))
        {
            return OperationResult.Failure(nameof(input.CategoryId), "Choose a category from the list.");
        }

        if (input.EventId is { } eventId && !await db.Events.AnyAsync(e => e.Id == eventId, cancellationToken))
        {
            return OperationResult.Failure(nameof(input.EventId), "Choose an event from the list.");
        }

        GalleryAlbum? album = null;
        if (input.Id is { } id)
        {
            album = await db.GalleryAlbums.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (album is null)
            {
                return OperationResult.Failure(string.Empty, "This album no longer exists.");
            }
        }

        if (album is null)
        {
            album = new GalleryAlbum();
            db.GalleryAlbums.Add(album);
        }

        album.CategoryId = input.CategoryId!.Value;
        album.Title = input.Title.Trim();
        album.TitleGu = AdminText.Optional(input.TitleGu);
        album.Slug = slug;
        album.Description = AdminText.Optional(input.Description);
        album.DescriptionGu = AdminText.Optional(input.DescriptionGu);
        album.TakenOn = input.TakenOn;
        album.EventId = input.EventId;
        album.IsPublished = input.IsPublished;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two people saving the same web address at the same moment: the unique index catches it.
            return OperationResult.Failure(nameof(input.Slug), "Another album already uses this web address.");
        }

        input.Id = album.Id;
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var album = await db.GalleryAlbums.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (album is null)
        {
            return OperationResult.Failure(string.Empty, "This album no longer exists.");
        }

        db.GalleryAlbums.Remove(album); // soft delete (AuditSaveChangesInterceptor); its photos are hidden with it
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<AlbumPhotosForm?> GetPhotosAsync(int albumId, CancellationToken cancellationToken = default)
    {
        var album = await db.GalleryAlbums.AsNoTracking()
            .Where(a => a.Id == albumId)
            .Select(a => new { a.Title, a.Slug, a.IsPublished })
            .SingleOrDefaultAsync(cancellationToken);
        if (album is null)
        {
            return null;
        }

        return new AlbumPhotosForm
        {
            AlbumId = albumId,
            AlbumTitle = album.Title,
            AlbumSlug = album.Slug,
            AlbumIsPublished = album.IsPublished,
            Photos = await db.GalleryImages.AsNoTracking()
                .Where(image => image.AlbumId == albumId)
                .OrderBy(image => image.SortOrder)
                .ThenBy(image => image.Id)
                .Select(image => new AlbumPhotoRow
                {
                    Id = image.Id,
                    ImagePath = image.ImagePath,
                    Status = image.Status,
                    UploadedBy = image.UploadedBy == null ? null
                        : image.UploadedBy.DisplayName != "" ? image.UploadedBy.DisplayName : image.UploadedBy.Email,
                    Caption = image.Caption,
                    CaptionGu = image.CaptionGu,
                    SortOrder = image.SortOrder
                })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PhotoUploadOutcome> AddPhotosAsync(
        int albumId, IReadOnlyList<IFormFile> files, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult();
        var chosen = files.Where(file => file.Length > 0).ToList();
        if (chosen.Count == 0)
        {
            return new PhotoUploadOutcome(result.AddError("NewPhotos", "Choose one or more photos first."), 0, false);
        }

        if (chosen.Count > MaxPhotosPerUpload)
        {
            return new PhotoUploadOutcome(result.AddError("NewPhotos", $"Choose at most {MaxPhotosPerUpload} photos at a time."), 0, false);
        }

        if (!await db.GalleryAlbums.AnyAsync(a => a.Id == albumId, cancellationToken))
        {
            return new PhotoUploadOutcome(result.AddError(string.Empty, "This album no longer exists."), 0, false);
        }

        var now = clock.UtcNow;
        var nextOrder = (await db.GalleryImages.Where(i => i.AlbumId == albumId).MaxAsync(i => (int?)i.SortOrder, cancellationToken) ?? 0) + 10;
        var saved = new List<StoredImage>();
        foreach (var file in chosen)
        {
            var upload = await images.SaveAsync(file, MediaFolder, cancellationToken);
            if (!upload.Succeeded)
            {
                result.AddError("NewPhotos", $"{Path.GetFileName(file.FileName)}: {upload.Error}");
                continue;
            }

            saved.Add(upload.Image!);
            db.GalleryImages.Add(new GalleryImage
            {
                AlbumId = albumId,
                ImagePath = upload.Image!.Path,
                Width = upload.Image.Width,
                Height = upload.Image.Height,
                SortOrder = nextOrder,
                UploadedByUserId = actor.UserId,
                // An Admin's own upload needs no second look; an Editor's waits for one.
                Status = actor.IsAdmin ? ModerationStatus.Published : ModerationStatus.Pending,
                ReviewedByUserId = actor.IsAdmin ? actor.UserId : null,
                ReviewedAt = actor.IsAdmin ? now : null
            });
            nextOrder += 10;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            saved.ForEach(image => images.Delete(image.Path));
            throw;
        }

        return new PhotoUploadOutcome(result, saved.Count, !actor.IsAdmin);
    }

    public async Task<OperationResult> UpdatePhotosAsync(int albumId, IReadOnlyList<AlbumPhotoRow> rows, CancellationToken cancellationToken = default)
    {
        // Only this album's photos: ids from the form are matched against the album, never trusted alone.
        var photos = await db.GalleryImages.Where(image => image.AlbumId == albumId).ToDictionaryAsync(image => image.Id, cancellationToken);
        var removeIds = new List<int>();
        foreach (var row in rows)
        {
            if (!photos.TryGetValue(row.Id, out var photo))
            {
                continue;
            }

            if (row.Remove)
            {
                removeIds.Add(photo.Id);
                continue;
            }

            photo.Caption = AdminText.Optional(row.Caption);
            photo.CaptionGu = AdminText.Optional(row.CaptionGu);
            photo.SortOrder = row.SortOrder;
        }

        await db.SaveChangesAsync(cancellationToken);
        await RemovePermanentlyAsync(removeIds, cancellationToken);
        return OperationResult.Success();
    }

    public async Task<PhotoReviewViewModel> GetReviewAsync(int? page, CancellationToken cancellationToken = default)
    {
        var waiting = db.GalleryImages.AsNoTracking()
            .Where(image => image.Status == ModerationStatus.Pending || image.Status == ModerationStatus.UnderReview);
        var total = await waiting.CountAsync(cancellationToken);
        var currentPage = PaginationViewModel.Clamp(page, total, ReviewPageSize);
        var items = await waiting
            .OrderBy(image => image.CreatedAt)
            .ThenBy(image => image.Id)
            .Skip((currentPage - 1) * ReviewPageSize)
            .Take(ReviewPageSize)
            .Select(image => new PhotoReviewItem(
                image.Id, image.ImagePath, image.Width, image.Height, image.Caption, image.AlbumId, image.Album.Title,
                image.UploadedBy == null ? null
                    : image.UploadedBy.DisplayName != "" ? image.UploadedBy.DisplayName : image.UploadedBy.Email,
                image.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PhotoReviewViewModel
        {
            Items = items,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = ReviewPageSize,
                TotalItems = total,
                RouteName = ReviewRouteName,
                RouteValues = new Dictionary<string, string?>()
            }
        };
    }

    public async Task<int> ReviewAsync(IReadOnlyList<int> photoIds, bool approve, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var ids = photoIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return 0;
        }

        // Only photos that are still waiting: a second Admin reviewing the same page cannot undo the first's work.
        var waiting = await db.GalleryImages
            .Where(image => ids.Contains(image.Id)
                            && (image.Status == ModerationStatus.Pending || image.Status == ModerationStatus.UnderReview))
            .ToListAsync(cancellationToken);
        if (!approve)
        {
            await RemovePermanentlyAsync(waiting.Select(image => image.Id).ToList(), cancellationToken);
            return waiting.Count;
        }

        var now = clock.UtcNow;
        foreach (var image in waiting)
        {
            image.Status = ModerationStatus.Published;
            image.ReviewedByUserId = actor.UserId;
            image.ReviewedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return waiting.Count;
    }

    private Task<int> CountWaitingAsync(CancellationToken cancellationToken) =>
        db.GalleryImages.CountAsync(image => image.Status == ModerationStatus.Pending || image.Status == ModerationStatus.UnderReview, cancellationToken);

    /// <summary>
    /// Deletes the rows for good (ExecuteDelete bypasses the soft-delete interceptor), then the files: rows first, so no
    /// row ever points at a missing file.
    /// </summary>
    private async Task RemovePermanentlyAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        // By id only (the callers checked album and status), so the DELETE needs no join for the query filter.
        var rows = db.GalleryImages.IgnoreQueryFilters().Where(image => ids.Contains(image.Id));
        var paths = await rows.Select(image => image.ImagePath).ToListAsync(cancellationToken);
        await rows.ExecuteDeleteAsync(cancellationToken);
        paths.ForEach(images.Delete);
    }
}
