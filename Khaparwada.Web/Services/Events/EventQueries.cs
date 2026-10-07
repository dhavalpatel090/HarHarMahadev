using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Events;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Events;

/// <summary>An event card as read from the database (times still as stored).</summary>
public sealed class EventCardRow
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public bool IsAllDay { get; init; }
    public string? Location { get; init; }
    public string? ImagePath { get; init; }
}

/// <summary>
/// The event card query, shared by Home, a place's page and /events, so an event reads the same everywhere.
/// Order, filter and page the events first, then select the cards.
/// </summary>
public static class EventQueries
{
    /// <summary>The venue from the Places list wins over the typed location; Gujarati falls back to English.</summary>
    public static IQueryable<EventCardRow> SelectCardRows(this IQueryable<Event> events, bool gujarati) =>
        events.Select(item => new EventCardRow
        {
            Slug = item.Slug,
            Title = gujarati ? item.TitleGu ?? item.Title : item.Title,
            Summary = gujarati ? item.SummaryGu ?? item.Summary : item.Summary,
            StartsAt = item.StartsAt,
            EndsAt = item.EndsAt,
            IsAllDay = item.IsAllDay,
            Location = item.Place != null
                ? gujarati ? item.Place.NameGu ?? item.Place.Name : item.Place.Name
                : gujarati ? item.LocationTextGu ?? item.LocationText : item.LocationText,
            ImagePath = item.ImagePath
        });

    /// <summary>Runs the query and shows the times in village time.</summary>
    public static async Task<List<EventCardViewModel>> ToCardsAsync(
        this IQueryable<EventCardRow> rows, IVillageClock clock, CancellationToken cancellationToken)
    {
        var list = await rows.ToListAsync(cancellationToken);
        return list.Select(row => new EventCardViewModel
        {
            Slug = row.Slug,
            Title = row.Title,
            Summary = row.Summary,
            StartsAt = clock.ToVillageTime(row.StartsAt),
            EndsAt = row.EndsAt is { } end ? clock.ToVillageTime(end) : null,
            IsAllDay = row.IsAllDay,
            Location = row.Location,
            ImagePath = row.ImagePath
        }).ToList();
    }
}
