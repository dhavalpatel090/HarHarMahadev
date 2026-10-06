using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Khaparwada.Web.Services.Notices;

public interface INoticeService
{
    /// <summary>Active urgent notices for the site-wide alert bar.</summary>
    Task<IReadOnlyList<NoticeAlertViewModel>> GetUrgentAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>Call after an admin saves a notice, so the alert bar updates immediately.</summary>
    void InvalidateAlerts();
}

public sealed class NoticeService(AppDbContext db, IMemoryCache cache, IVillageClock clock) : INoticeService
{
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

    private sealed record AlertRow(string Slug, string Title, string? TitleGu);
}
