using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Events;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Events;

public interface IEventService
{
    Task<EventListViewModel> GetListAsync(EventListView view, int? page, CancellationToken cancellationToken = default);
    Task<EventDetailsViewModel?> GetDetailsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>For the original plan's address /Events/Details/{id}.</summary>
    Task<string?> GetSlugAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>The "Add to calendar" file; <paramref name="pageUrl"/> is the event page's full address.</summary>
    Task<EventCalendarFile?> GetCalendarAsync(string slug, Uri pageUrl, CancellationToken cancellationToken = default);
}

/// <summary>The public event pages. Every query goes through PublicVisibility (published; upcoming or past).</summary>
public sealed class EventService(AppDbContext db, IVillageClock clock) : IEventService
{
    public const int PageSize = 12;

    public async Task<EventListViewModel> GetListAsync(EventListView view, int? page, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var events = db.Events.AsNoTracking();
        var upcomingCount = await events.Upcoming(now).CountAsync(cancellationToken);
        var pastCount = await events.Past(now).CountAsync(cancellationToken);

        // Upcoming: soonest first. Past: most recent first, which is what people look for.
        var query = view == EventListView.Past
            ? events.Past(now).OrderByDescending(item => item.StartsAt).ThenBy(item => item.Title)
            : events.Upcoming(now).OrderBy(item => item.StartsAt).ThenBy(item => item.Title);
        var total = view == EventListView.Past ? pastCount : upcomingCount;
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var cards = await query
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .SelectCardRows(AppLanguage.IsGujarati)
            .ToCardsAsync(clock, cancellationToken);

        return new EventListViewModel
        {
            View = view,
            Events = cards,
            UpcomingCount = upcomingCount,
            PastCount = pastCount,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = RouteNames.Events,
                RouteValues = new Dictionary<string, string?> { ["show"] = view == EventListView.Past ? "past" : null }
            }
        };
    }

    public async Task<EventDetailsViewModel?> GetDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var item = await db.Events.AsNoTracking()
            .Visible()
            .Where(e => e.Slug == slug)
            .Select(e => new
            {
                e.Id,
                e.Slug,
                Title = gujarati ? e.TitleGu ?? e.Title : e.Title,
                Summary = gujarati ? e.SummaryGu ?? e.Summary : e.Summary,
                Description = gujarati ? e.DescriptionGu ?? e.Description : e.Description,
                e.StartsAt,
                e.EndsAt,
                e.IsAllDay,
                e.ImagePath,
                LocationText = gujarati ? e.LocationTextGu ?? e.LocationText : e.LocationText,
                VenueName = e.Place != null ? gujarati ? e.Place.NameGu ?? e.Place.Name : e.Place.Name : null,
                VenueIsPublic = e.Place != null && e.Place.IsPublished && e.Place.Category.IsActive,
                VenueSlug = e.Place != null ? e.Place.Slug : null,
                VenueAddress = e.Place != null ? gujarati ? e.Place.AddressGu ?? e.Place.Address : e.Place.Address : null,
                Latitude = e.Place != null ? e.Place.Latitude : null,
                Longitude = e.Place != null ? e.Place.Longitude : null
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var now = clock.UtcNow;
        var state = now >= EventTiming.EndOf(item.StartsAt, item.EndsAt, item.IsAllDay) ? EventState.Past
            : now >= item.StartsAt ? EventState.Now
            : EventState.Upcoming;

        var more = await db.Events.AsNoTracking()
            .Upcoming(now)
            .Where(e => e.Id != item.Id)
            .OrderBy(e => e.StartsAt)
            .Take(3)
            .SelectCardRows(gujarati)
            .ToCardsAsync(clock, cancellationToken);

        // A hidden venue keeps its name, but no link, address or directions: those belong to its page.
        return new EventDetailsViewModel
        {
            Slug = item.Slug,
            Title = item.Title,
            Summary = item.Summary,
            Description = item.Description,
            StartsAt = clock.ToVillageTime(item.StartsAt),
            EndsAt = item.EndsAt is { } end ? clock.ToVillageTime(end) : null,
            IsAllDay = item.IsAllDay,
            State = state,
            ImagePath = item.ImagePath,
            VenueName = item.VenueName,
            VenueSlug = item.VenueIsPublic ? item.VenueSlug : null,
            VenueAddress = item.VenueIsPublic ? item.VenueAddress : null,
            Latitude = item.VenueIsPublic ? item.Latitude : null,
            Longitude = item.VenueIsPublic ? item.Longitude : null,
            LocationText = item.LocationText,
            MoreEvents = more
        };
    }

    public Task<string?> GetSlugAsync(int id, CancellationToken cancellationToken = default) =>
        db.Events.AsNoTracking()
            .Visible()
            .Where(e => e.Id == id)
            .Select(e => e.Slug)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<EventCalendarFile?> GetCalendarAsync(string slug, Uri pageUrl, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var item = await db.Events.AsNoTracking()
            .Visible()
            .Where(e => e.Slug == slug)
            .Select(e => new
            {
                e.Id,
                e.Slug,
                Title = gujarati ? e.TitleGu ?? e.Title : e.Title,
                Summary = gujarati ? e.SummaryGu ?? e.Summary : e.Summary,
                e.StartsAt,
                e.EndsAt,
                e.IsAllDay,
                Venue = e.Place != null ? gujarati ? e.Place.NameGu ?? e.Place.Name : e.Place.Name : null,
                LocationText = gujarati ? e.LocationTextGu ?? e.LocationText : e.LocationText
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var location = string.Join(", ", new[] { item.Venue, item.LocationText }.Where(text => !string.IsNullOrWhiteSpace(text)));
        var data = new EventCalendarData(
            item.Id, item.Title, item.Summary,
            clock.ToVillageTime(item.StartsAt), item.EndsAt is { } end ? clock.ToVillageTime(end) : null, item.IsAllDay,
            location.Length == 0 ? null : location, pageUrl);
        return new EventCalendarFile(item.Slug + ".ics", EventCalendar.Build(data, clock.UtcNow));
    }
}
