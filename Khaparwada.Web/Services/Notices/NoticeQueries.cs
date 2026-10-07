using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Notices;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Notices;

/// <summary>The notice board's order and list items, shared by Home and /notices.</summary>
public static class NoticeQueries
{
    /// <summary>
    /// Urgent first, then important, then newest. Priority is stored as text, so ORDER BY Priority would sort
    /// alphabetically ("Important", "Normal", "Urgent"); it is ranked explicitly.
    /// </summary>
    public static IOrderedQueryable<Notice> InBoardOrder(this IQueryable<Notice> notices) =>
        notices.OrderByDescending(notice => notice.Priority == NoticePriority.Urgent)
            .ThenByDescending(notice => notice.Priority == NoticePriority.Important)
            .ThenByDescending(notice => notice.PublishedAt)
            .ThenByDescending(notice => notice.Id);

    /// <summary>Runs the query; times in village time. With excerpts the text is read too (the board shows it).</summary>
    public static async Task<List<NoticeItemViewModel>> ToItemsAsync(
        this IQueryable<Notice> notices, bool gujarati, bool withExcerpt, IVillageClock clock, CancellationToken cancellationToken)
    {
        var rows = await notices
            .Select(notice => new
            {
                notice.Slug,
                Title = gujarati ? notice.TitleGu ?? notice.Title : notice.Title,
                Description = withExcerpt ? gujarati ? notice.DescriptionGu ?? notice.Description : notice.Description : null,
                notice.Category,
                notice.Priority,
                notice.PublishedAt,
                notice.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new NoticeItemViewModel
        {
            Slug = row.Slug,
            Title = row.Title,
            Category = row.Category,
            Priority = row.Priority,
            PublishedAt = clock.ToVillageTime(row.PublishedAt),
            ValidUntil = NoticeDisplay.LastDay(row.ExpiresAt is { } expires ? clock.ToVillageTime(expires) : null),
            Excerpt = row.Description is null ? null : TextHelpers.Excerpt(row.Description, 180)
        }).ToList();
    }
}
