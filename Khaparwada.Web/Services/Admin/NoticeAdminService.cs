using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.Services.Notices;
using Khaparwada.Web.ViewModels.Notices;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface INoticeAdminService
{
    Task<NoticeAdminListViewModel> ListAsync(NoticeListFilter filter, string? search, int? page, CancellationToken cancellationToken = default);

    /// <summary>A new notice's form: shown from now, no end.</summary>
    NoticeInputModel NewForm();

    Task<NoticeInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the notice when input.Id is null, otherwise updates it; the urgent bar is refreshed.</summary>
    Task<OperationResult> SaveAsync(NoticeInputModel input, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the notice leaves the board (and the urgent bar) at once.</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The attachment of any notice, for the admins' preview (the public link works only while it is shown).</summary>
    Task<NoticeAttachmentFile?> GetAttachmentAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class NoticeAdminService(
    AppDbContext db,
    IImageUploadService images,
    IDocumentUploadService documents,
    IUploadStorage storage,
    INoticeService notices,
    IVillageClock clock) : INoticeAdminService
{
    public const int PageSize = 25;
    private const string MediaFolder = "notices";
    private const string ListRouteName = "admin-notices";

    /// <summary>Room for a "-999" after a generated address.</summary>
    private const int SlugStemLength = FieldLengths.Slug - 10;

    public async Task<NoticeAdminListViewModel> ListAsync(
        NoticeListFilter filter, string? search, int? page, CancellationToken cancellationToken = default)
    {
        search = AdminText.Optional(search);
        var now = clock.UtcNow;
        var query = db.Notices.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(n => n.Title.Contains(search) || (n.TitleGu != null && n.TitleGu.Contains(search)));
        }

        // The tabs, from the dates (see NoticeState): hidden first, then not yet started, ended, on the board.
        var hidden = query.Where(n => !n.IsPublished);
        var scheduled = query.Where(n => n.IsPublished && n.PublishedAt > now);
        var ended = query.Where(n => n.IsPublished && n.PublishedAt <= now && n.ExpiresAt != null && n.ExpiresAt <= now);
        var active = query.Active(now);
        var counts = new Dictionary<NoticeListFilter, int>
        {
            [NoticeListFilter.Active] = await active.CountAsync(cancellationToken),
            [NoticeListFilter.Scheduled] = await scheduled.CountAsync(cancellationToken),
            [NoticeListFilter.Ended] = await ended.CountAsync(cancellationToken),
            [NoticeListFilter.Hidden] = await hidden.CountAsync(cancellationToken)
        };
        counts[NoticeListFilter.All] = counts.Values.Sum();

        // Each tab in the order that helps: the board's own order, the next to start, the latest to end.
        var ordered = filter switch
        {
            NoticeListFilter.Active => active.InBoardOrder(),
            NoticeListFilter.Scheduled => scheduled.OrderBy(n => n.PublishedAt).ThenBy(n => n.Id),
            NoticeListFilter.Ended => ended.OrderByDescending(n => n.ExpiresAt).ThenByDescending(n => n.Id),
            NoticeListFilter.Hidden => hidden.OrderByDescending(n => n.PublishedAt).ThenByDescending(n => n.Id),
            _ => query.OrderByDescending(n => n.PublishedAt).ThenByDescending(n => n.Id)
        };

        var total = counts[filter];
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var rows = await ordered
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.TitleGu,
                n.Category,
                n.Priority,
                n.PublishedAt,
                n.ExpiresAt,
                n.IsPublished,
                HasAttachment = n.AttachmentPath != null,
                IsPlaceholder = n.Title.StartsWith("[") || n.Description.StartsWith("[")
            })
            .ToListAsync(cancellationToken);

        return new NoticeAdminListViewModel
        {
            Items = rows.Select(row => new NoticeListItem(
                row.Id, row.Title, row.TitleGu, row.Category, row.Priority, clock.ToVillageTime(row.PublishedAt),
                NoticeDisplay.LastDay(row.ExpiresAt is { } expires ? clock.ToVillageTime(expires) : null),
                !row.IsPublished ? NoticeState.Hidden
                    : row.PublishedAt > now ? NoticeState.Scheduled
                    : row.ExpiresAt is { } end && end <= now ? NoticeState.Ended
                    : NoticeState.Active,
                row.HasAttachment, row.IsPlaceholder)).ToList(),
            Counts = counts,
            Filter = filter,
            Search = search,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = ListRouteName,
                RouteValues = new Dictionary<string, string?>
                {
                    ["show"] = filter == NoticeListFilter.Active ? null : filter.ToString().ToLowerInvariant(),
                    ["q"] = search
                }
            }
        };
    }

    public NoticeInputModel NewForm()
    {
        var now = clock.ToVillageTime(clock.UtcNow).DateTime;
        return new NoticeInputModel
        {
            PublishDate = DateOnly.FromDateTime(now),
            PublishTime = new TimeOnly(now.Hour, now.Minute)
        };
    }

    public async Task<NoticeInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var notice = await db.Notices.AsNoTracking().SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (notice is null)
        {
            return null;
        }

        var published = clock.ToVillageTime(notice.PublishedAt).DateTime;
        return new NoticeInputModel
        {
            Id = notice.Id,
            Title = notice.Title,
            TitleGu = notice.TitleGu,
            Slug = notice.Slug,
            Description = notice.Description,
            DescriptionGu = notice.DescriptionGu,
            Category = notice.Category,
            Priority = notice.Priority,
            PublishDate = DateOnly.FromDateTime(published),
            PublishTime = new TimeOnly(published.Hour, published.Minute),
            LastDay = NoticeDisplay.LastDay(notice.ExpiresAt is { } expires ? clock.ToVillageTime(expires) : null),
            CurrentImageUrl = MediaUrls.Image(notice.ImagePath),
            CurrentAttachmentName = notice.AttachmentPath is null ? null : notice.AttachmentName ?? "document.pdf",
            IsPublished = notice.IsPublished
        };
    }

    public async Task<OperationResult> SaveAsync(NoticeInputModel input, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(input.Category))
        {
            return OperationResult.Failure(nameof(input.Category), "Choose a kind from the list.");
        }

        if (!Enum.IsDefined(input.Priority))
        {
            return OperationResult.Failure(nameof(input.Priority), "Choose a priority from the list.");
        }

        if (input.PublishDate is not { } publishDate || input.PublishTime is not { } publishTime)
        {
            return OperationResult.Failure(nameof(input.PublishDate), "Choose the date and time.");
        }

        var publishedAt = clock.FromVillageTime(publishDate, publishTime);
        DateTimeOffset? expiresAt = input.LastDay is { } lastDay ? clock.FromVillageTime(lastDay.AddDays(1), TimeOnly.MinValue) : null;
        if (expiresAt <= publishedAt)
        {
            return OperationResult.Failure(nameof(input.LastDay), "The last day cannot be before the day the notice is shown from.");
        }

        Notice? notice = null;
        if (input.Id is { } id)
        {
            notice = await db.Notices.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
            if (notice is null)
            {
                return OperationResult.Failure(string.Empty, "This notice no longer exists.");
            }
        }

        // A typed address must be free; a generated one is made free ("water-supply-update-2"), because notices
        // with the same title come back often (a monthly water update).
        string slug;
        if (string.IsNullOrWhiteSpace(input.Slug))
        {
            slug = await FreeSlugAsync(SlugGenerator.Create(input.Title), input.Id, cancellationToken);
            if (!SlugGenerator.IsValid(slug))
            {
                return OperationResult.Failure(nameof(input.Slug), "Enter a web address in English letters, e.g. water-supply-update.");
            }
        }
        else
        {
            slug = input.Slug.Trim();
            if (await db.Notices.AnyAsync(n => n.Slug == slug && n.Id != input.Id, cancellationToken))
            {
                return OperationResult.Failure(nameof(input.Slug), "Another notice already uses this web address.");
            }
        }

        var image = await ImageEdit.PrepareAsync(images, input.Image, input.RemoveImage, MediaFolder, cancellationToken);
        if (image.Failed)
        {
            return OperationResult.Failure(nameof(input.Image), image.Error!);
        }

        // The attachment, the same way as ImageEdit: save the new file first, delete the replaced one only after
        // the database save (or the new one if the save fails).
        StoredDocument? newDocument = null;
        if (input.Attachment is { Length: > 0 } file)
        {
            var upload = await documents.SaveAsync(file, MediaFolder, cancellationToken);
            if (!upload.Succeeded)
            {
                image.SaveFailed();
                return OperationResult.Failure(nameof(input.Attachment), upload.Error!);
            }

            newDocument = upload.Document;
        }

        if (notice is null)
        {
            notice = new Notice();
            db.Notices.Add(notice);
        }

        var replacedDocument = newDocument is not null || input.RemoveAttachment ? notice.AttachmentPath : null;
        notice.Title = input.Title.Trim();
        notice.TitleGu = AdminText.Optional(input.TitleGu);
        notice.Slug = slug;
        notice.Description = input.Description.Trim();
        notice.DescriptionGu = AdminText.Optional(input.DescriptionGu);
        notice.Category = input.Category;
        notice.Priority = input.Priority;
        notice.PublishedAt = publishedAt;
        notice.ExpiresAt = expiresAt;
        notice.ImagePath = image.Apply(notice.ImagePath);
        if (newDocument is not null)
        {
            notice.AttachmentPath = newDocument.Path;
            notice.AttachmentName = newDocument.FileName;
        }
        else if (input.RemoveAttachment)
        {
            notice.AttachmentPath = null;
            notice.AttachmentName = null;
        }

        notice.IsPublished = input.IsPublished;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two people saving the same web address at the same moment: the unique index catches it.
            image.SaveFailed();
            documents.Delete(newDocument?.Path);
            return OperationResult.Failure(nameof(input.Slug), "Another notice already uses this web address.");
        }
        catch
        {
            image.SaveFailed();
            documents.Delete(newDocument?.Path);
            throw;
        }

        image.SaveSucceeded();
        documents.Delete(replacedDocument);
        notices.InvalidateAlerts(); // an urgent notice shows (or leaves) the alert bar at once, not after a minute

        var now = clock.UtcNow;
        input.SavedState = !notice.IsPublished ? NoticeState.Hidden
            : publishedAt > now ? NoticeState.Scheduled
            : expiresAt <= now ? NoticeState.Ended
            : NoticeState.Active;
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var notice = await db.Notices.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (notice is null)
        {
            return OperationResult.Failure(string.Empty, "This notice no longer exists.");
        }

        db.Notices.Remove(notice); // soft delete (AuditSaveChangesInterceptor); its files stay for a restore
        await db.SaveChangesAsync(cancellationToken);
        notices.InvalidateAlerts();
        return OperationResult.Success();
    }

    public async Task<NoticeAttachmentFile?> GetAttachmentAsync(int id, CancellationToken cancellationToken = default)
    {
        var notice = await db.Notices.AsNoTracking()
            .Where(n => n.Id == id && n.AttachmentPath != null)
            .Select(n => new { n.AttachmentPath, n.AttachmentName })
            .SingleOrDefaultAsync(cancellationToken);
        if (notice is null)
        {
            return null;
        }

        var fullPath = storage.PrivatePath(notice.AttachmentPath!);
        return File.Exists(fullPath) ? new NoticeAttachmentFile(fullPath, notice.AttachmentName ?? "document.pdf") : null;
    }

    /// <summary>"water-supply-update", or "-2", "-3"… after it when taken (soft-deleted notices keep theirs free).</summary>
    private async Task<string> FreeSlugAsync(string baseSlug, int? exceptId, CancellationToken cancellationToken)
    {
        if (baseSlug.Length == 0)
        {
            return baseSlug;
        }

        var stem = baseSlug.Length > SlugStemLength ? baseSlug[..SlugStemLength].TrimEnd('-') : baseSlug;
        var taken = await db.Notices
            .Where(n => n.Id != exceptId && (n.Slug == stem || n.Slug.StartsWith(stem + "-")))
            .Select(n => n.Slug)
            .ToListAsync(cancellationToken);
        if (!taken.Contains(stem))
        {
            return stem;
        }

        for (var number = 2; ; number++)
        {
            var candidate = $"{stem}-{number}";
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
