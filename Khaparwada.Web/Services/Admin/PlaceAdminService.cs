using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface IPlaceAdminService
{
    Task<PlaceAdminListViewModel> ListAsync(int? categoryId, string? search, int? page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PlaceInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the place when input.Id is null, otherwise updates it.</summary>
    Task<OperationResult> SaveAsync(PlaceInputModel input, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the place leaves the website and the map; the row and its photos stay recoverable.</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<PlacePhotosForm?> GetPhotosAsync(int placeId, CancellationToken cancellationToken = default);

    /// <summary>Adds photos to the place's gallery; each refused file gets its own message.</summary>
    Task<OperationResult> AddPhotosAsync(int placeId, IReadOnlyList<IFormFile> files, CancellationToken cancellationToken = default);

    /// <summary>Saves captions and order, and removes the ticked photos (rows and files).</summary>
    Task<OperationResult> UpdatePhotosAsync(int placeId, IReadOnlyList<PlacePhotoRow> rows, CancellationToken cancellationToken = default);
}

public sealed class PlaceAdminService(AppDbContext db, IImageUploadService images) : IPlaceAdminService
{
    public const int PageSize = 25;
    public const int MaxPhotosPerUpload = 6;
    private const string MediaFolder = "places";
    private const string ListRouteName = "admin-places";

    public async Task<PlaceAdminListViewModel> ListAsync(int? categoryId, string? search, int? page, CancellationToken cancellationToken = default)
    {
        search = AdminText.Optional(search);
        var query = db.Places.AsNoTracking();
        if (categoryId is { } id)
        {
            query = query.Where(place => place.CategoryId == id);
        }

        if (search is not null)
        {
            query = query.Where(place => place.Name.Contains(search) || (place.NameGu != null && place.NameGu.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var items = await query
            .OrderBy(place => place.Category.SortOrder)
            .ThenBy(place => place.Name)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(place => new PlaceListItem(
                place.Id, place.Name, place.NameGu, place.Slug, place.Category.Name, place.Category.IconClass, place.Category.MarkerColor,
                place.MainImagePath, place.Latitude != null, place.IsFeatured, place.IsPublished,
                place.Name.StartsWith("[") || place.Summary.StartsWith("["), place.Images.Count()))
            .ToListAsync(cancellationToken);

        return new PlaceAdminListViewModel
        {
            Items = items,
            Categories = await GetCategoriesAsync(cancellationToken),
            CategoryId = categoryId,
            Search = search,
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
        await db.PlaceCategories.AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new PlaceCategoryOption(category.Id, category.Name, category.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<PlaceInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var place = await db.Places.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { Place = p, PhotoCount = p.Images.Count() })
            .SingleOrDefaultAsync(cancellationToken);
        if (place is null)
        {
            return null;
        }

        var p = place.Place;
        return new PlaceInputModel
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            Name = p.Name,
            NameGu = p.NameGu,
            Slug = p.Slug,
            Summary = p.Summary,
            SummaryGu = p.SummaryGu,
            Description = p.Description,
            DescriptionGu = p.DescriptionGu,
            CurrentImageUrl = MediaUrls.Image(p.MainImagePath),
            Address = p.Address,
            AddressGu = p.AddressGu,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            Phone = p.Phone,
            Email = p.Email,
            Website = p.Website,
            Timings = p.Timings,
            TimingsGu = p.TimingsGu,
            IsFeatured = p.IsFeatured,
            IsPublished = p.IsPublished,
            PhotoCount = place.PhotoCount
        };
    }

    public async Task<OperationResult> SaveAsync(PlaceInputModel input, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Create(input.Name) : input.Slug.Trim();
        if (!SlugGenerator.IsValid(slug))
        {
            return OperationResult.Failure(nameof(input.Slug), "Enter a web address in English letters, e.g. village-lake.");
        }

        if (await db.Places.AnyAsync(p => p.Slug == slug && p.Id != input.Id, cancellationToken))
        {
            return OperationResult.Failure(nameof(input.Slug), "Another place already uses this web address.");
        }

        if (!await db.PlaceCategories.AnyAsync(c => c.Id == input.CategoryId, cancellationToken))
        {
            return OperationResult.Failure(nameof(input.CategoryId), "Choose a category from the list.");
        }

        Place? place = null;
        if (input.Id is { } id)
        {
            place = await db.Places.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (place is null)
            {
                return OperationResult.Failure(string.Empty, "This place no longer exists.");
            }
        }

        var image = await ImageEdit.PrepareAsync(images, input.Image, input.RemoveImage, MediaFolder, cancellationToken);
        if (image.Failed)
        {
            return OperationResult.Failure(nameof(input.Image), image.Error!);
        }

        if (place is null)
        {
            place = new Place();
            db.Places.Add(place);
        }

        place.CategoryId = input.CategoryId!.Value;
        place.Name = input.Name.Trim();
        place.NameGu = AdminText.Optional(input.NameGu);
        place.Slug = slug;
        place.Summary = input.Summary.Trim();
        place.SummaryGu = AdminText.Optional(input.SummaryGu);
        place.Description = AdminText.Optional(input.Description);
        place.DescriptionGu = AdminText.Optional(input.DescriptionGu);
        place.MainImagePath = image.Apply(place.MainImagePath);
        place.Address = AdminText.Optional(input.Address);
        place.AddressGu = AdminText.Optional(input.AddressGu);
        place.Latitude = input.Latitude is { } latitude ? Math.Round(latitude, 6, MidpointRounding.AwayFromZero) : null;
        place.Longitude = input.Longitude is { } longitude ? Math.Round(longitude, 6, MidpointRounding.AwayFromZero) : null;
        place.Phone = AdminText.Optional(input.Phone);
        place.Email = AdminText.Optional(input.Email);
        place.Website = AdminText.Optional(input.Website);
        place.Timings = AdminText.Optional(input.Timings);
        place.TimingsGu = AdminText.Optional(input.TimingsGu);
        place.IsFeatured = input.IsFeatured;
        place.IsPublished = input.IsPublished;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two admins saving the same web address at the same moment: the unique index catches it.
            image.SaveFailed();
            return OperationResult.Failure(nameof(input.Slug), "Another place already uses this web address.");
        }
        catch
        {
            image.SaveFailed();
            throw;
        }

        image.SaveSucceeded();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var place = await db.Places.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (place is null)
        {
            return OperationResult.Failure(string.Empty, "This place no longer exists.");
        }

        db.Places.Remove(place); // becomes a soft delete (AuditSaveChangesInterceptor); photos stay for a restore
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    public async Task<PlacePhotosForm?> GetPhotosAsync(int placeId, CancellationToken cancellationToken = default)
    {
        var place = await db.Places.AsNoTracking()
            .Where(p => p.Id == placeId)
            .Select(p => new { p.Name, p.Slug })
            .SingleOrDefaultAsync(cancellationToken);
        if (place is null)
        {
            return null;
        }

        return new PlacePhotosForm
        {
            PlaceId = placeId,
            PlaceName = place.Name,
            PlaceSlug = place.Slug,
            Photos = await db.PlaceImages.AsNoTracking()
                .Where(image => image.PlaceId == placeId)
                .OrderBy(image => image.SortOrder)
                .ThenBy(image => image.Id)
                .Select(image => new PlacePhotoRow
                {
                    Id = image.Id,
                    ImagePath = image.ImagePath,
                    Caption = image.Caption,
                    CaptionGu = image.CaptionGu,
                    SortOrder = image.SortOrder
                })
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<OperationResult> AddPhotosAsync(int placeId, IReadOnlyList<IFormFile> files, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult();
        var chosen = files.Where(file => file.Length > 0).ToList();
        if (chosen.Count == 0)
        {
            return result.AddError("NewPhotos", "Choose one or more photos first.");
        }

        if (chosen.Count > MaxPhotosPerUpload)
        {
            return result.AddError("NewPhotos", $"Choose at most {MaxPhotosPerUpload} photos at a time.");
        }

        if (!await db.Places.AnyAsync(p => p.Id == placeId, cancellationToken))
        {
            return result.AddError(string.Empty, "This place no longer exists.");
        }

        var nextOrder = (await db.PlaceImages.Where(i => i.PlaceId == placeId).MaxAsync(i => (int?)i.SortOrder, cancellationToken) ?? 0) + 10;
        var saved = new List<StoredImage>();
        foreach (var file in chosen)
        {
            var upload = await images.SaveAsync(file, MediaFolder, cancellationToken);
            if (upload.Succeeded)
            {
                saved.Add(upload.Image!);
                db.PlaceImages.Add(new PlaceImage
                {
                    PlaceId = placeId,
                    ImagePath = upload.Image!.Path,
                    Width = upload.Image.Width,
                    Height = upload.Image.Height,
                    SortOrder = nextOrder
                });
                nextOrder += 10;
            }
            else
            {
                result.AddError("NewPhotos", $"{Path.GetFileName(file.FileName)}: {upload.Error}");
            }
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

        return result;
    }

    public async Task<OperationResult> UpdatePhotosAsync(int placeId, IReadOnlyList<PlacePhotoRow> rows, CancellationToken cancellationToken = default)
    {
        // Only this place's photos: ids from the form are matched against the place, never trusted alone.
        var photos = await db.PlaceImages.Where(image => image.PlaceId == placeId).ToDictionaryAsync(image => image.Id, cancellationToken);
        var removed = new List<string>();
        foreach (var row in rows)
        {
            if (!photos.TryGetValue(row.Id, out var photo))
            {
                continue;
            }

            if (row.Remove)
            {
                removed.Add(photo.ImagePath);
                db.PlaceImages.Remove(photo);
                continue;
            }

            photo.Caption = AdminText.Optional(row.Caption);
            photo.CaptionGu = AdminText.Optional(row.CaptionGu);
            photo.SortOrder = row.SortOrder;
        }

        await db.SaveChangesAsync(cancellationToken);
        removed.ForEach(images.Delete); // only after the rows are gone, so no row ever points at a missing file
        return OperationResult.Success();
    }
}
