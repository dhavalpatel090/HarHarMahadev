using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.ViewModels.Notices;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Khaparwada.Web.Services.Notices;

public interface INoticeService
{
    /// <summary>Active urgent notices for the site-wide alert bar.</summary>
    Task<IReadOnlyList<NoticeAlertViewModel>> GetUrgentAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>Call after an admin saves a notice, so the alert bar updates immediately.</summary>
    void InvalidateAlerts();

    /// <summary>/notices: the notices on the board now, optionally of one category.</summary>
    Task<NoticeListViewModel> GetListAsync(string? category, int? page, CancellationToken cancellationToken = default);

    /// <summary>A notice on the board, one that has ended (title only), or nothing.</summary>
    Task<NoticeLookup> GetDetailsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>For the original plan's address /Notices/Details/{id}: published notices only.</summary>
    Task<string?> GetSlugAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The attachment of a notice that is on the board now; null otherwise.</summary>
    Task<NoticeAttachmentFile?> GetAttachmentAsync(string slug, CancellationToken cancellationToken = default);
}

/// <summary>
/// The public notice board. "On the board" (PublicVisibility.Active) = published, already started, not yet ended.
/// Ended notices never show their text again: it may be out of date (an old water timing passed on as current).
/// </summary>
public sealed class NoticeService(AppDbContext db, IMemoryCache cache, IVillageClock clock, IUploadStorage storage) : INoticeService
{
    public const int PageSize = 15;
    private const string AlertsCacheKey = "urgent-notices";

    // The alert bar is on every page; one minute of caching keeps it to one query a minute.
    private static readonly TimeSpan AlertsCacheDuration = TimeSpan.FromMinutes(1);

    public async Task<IReadOnlyList<NoticeAlertViewModel>> GetUrgentAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await cache.GetOrCreateAsync(AlertsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = AlertsCacheDuration;
            return await db.Notices.AsNoTracking()
                .Active(clock.UtcNow)
                .Where(notice => notice.Priority == NoticePriority.Urgent)
                .OrderByDescending(notice => notice.PublishedAt)
                .Select(notice => new AlertRow(notice.Slug, notice.Title, notice.TitleGu))
                .Take(3)
                .ToListAsync(cancellationToken);
        });

        return alerts!.Select(alert => new NoticeAlertViewModel(alert.Slug, AppLanguage.Pick(alert.Title, alert.TitleGu))).ToList();
    }

    public void InvalidateAlerts() => cache.Remove(AlertsCacheKey);

    public async Task<NoticeListViewModel> GetListAsync(string? category, int? page, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var active = db.Notices.AsNoTracking().Active(now);
        var counts = await active
            .GroupBy(notice => notice.Category)
            .Select(group => new { Category = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Category, group => group.Count, cancellationToken);
        var categories = Enum.GetValues<NoticeCategory>()
            .Select(value => new NoticeCategoryViewModel(value, counts.GetValueOrDefault(value)))
            .ToList();
        var selected = NoticeDisplay.FromSlug(category) is { } chosen ? categories.Single(c => c.Category == chosen) : null;

        var query = selected is null ? active : active.Where(notice => notice.Category == selected.Category);
        var total = selected?.Count ?? counts.Values.Sum();
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var notices = await query
            .InBoardOrder()
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToItemsAsync(AppLanguage.IsGujarati, withExcerpt: true, clock, cancellationToken);

        return new NoticeListViewModel
        {
            Categories = categories,
            Category = selected,
            Notices = notices,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = RouteNames.Notices,
                RouteValues = new Dictionary<string, string?> { ["category"] = selected?.Slug }
            }
        };
    }

    public async Task<NoticeLookup> GetDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var now = clock.UtcNow;
        var notice = await db.Notices.AsNoTracking()
            .Where(n => n.Slug == slug && n.IsPublished && n.PublishedAt <= now)
            .Select(n => new
            {
                n.Id,
                n.Slug,
                Title = gujarati ? n.TitleGu ?? n.Title : n.Title,
                Description = gujarati ? n.DescriptionGu ?? n.Description : n.Description,
                n.Category,
                n.Priority,
                n.PublishedAt,
                n.ExpiresAt,
                n.ImagePath,
                n.AttachmentPath,
                n.AttachmentName
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (notice is null)
        {
            return new NoticeLookup(NoticeLookupState.Missing);
        }

        var validUntil = NoticeDisplay.LastDay(notice.ExpiresAt is { } expires ? clock.ToVillageTime(expires) : null);
        if (notice.ExpiresAt is { } end && end <= now)
        {
            return new NoticeLookup(NoticeLookupState.Ended, EndedTitle: notice.Title, EndedOn: validUntil);
        }

        var more = await db.Notices.AsNoTracking()
            .Active(now)
            .Where(n => n.Id != notice.Id)
            .InBoardOrder()
            .Take(4)
            .ToItemsAsync(gujarati, withExcerpt: false, clock, cancellationToken);

        long? attachmentBytes = null;
        if (notice.AttachmentPath is not null)
        {
            var file = new FileInfo(storage.PrivatePath(notice.AttachmentPath));
            attachmentBytes = file.Exists ? file.Length : null;
        }

        return new NoticeLookup(NoticeLookupState.Active, new NoticeDetailsViewModel
        {
            Slug = notice.Slug,
            Title = notice.Title,
            Description = notice.Description,
            Category = notice.Category,
            Priority = notice.Priority,
            PublishedAt = clock.ToVillageTime(notice.PublishedAt),
            ValidUntil = validUntil,
            ImagePath = notice.ImagePath,
            // A missing file (restored database without its files) shows no broken link.
            AttachmentName = attachmentBytes is null ? null : notice.AttachmentName ?? "document.pdf",
            AttachmentBytes = attachmentBytes,
            MoreNotices = more
        });
    }

    public Task<string?> GetSlugAsync(int id, CancellationToken cancellationToken = default) =>
        db.Notices.AsNoTracking()
            .Where(n => n.Id == id && n.IsPublished)
            .Select(n => n.Slug)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<NoticeAttachmentFile?> GetAttachmentAsync(string slug, CancellationToken cancellationToken = default)
    {
        var notice = await db.Notices.AsNoTracking()
            .Active(clock.UtcNow)
            .Where(n => n.Slug == slug && n.AttachmentPath != null)
            .Select(n => new { n.AttachmentPath, n.AttachmentName })
            .SingleOrDefaultAsync(cancellationToken);
        if (notice is null)
        {
            return null;
        }

        var fullPath = storage.PrivatePath(notice.AttachmentPath!);
        return File.Exists(fullPath) ? new NoticeAttachmentFile(fullPath, notice.AttachmentName ?? "document.pdf") : null;
    }

    private sealed record AlertRow(string Slug, string Title, string? TitleGu);
}
