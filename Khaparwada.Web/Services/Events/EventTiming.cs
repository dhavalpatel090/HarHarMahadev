using Khaparwada.Web.Models.Entities;

namespace Khaparwada.Web.Services.Events;

/// <summary>
/// When an event is over, in one place, for both the database queries and C#:
/// <list type="bullet">
/// <item>an all-day event lasts until the end of its last day (EndsAt holds that day at midnight, or is empty for a
/// one-day event);</item>
/// <item>a timed event lasts until EndsAt, or <see cref="DefaultHours"/> hours when it has no end time, otherwise a
/// 9 PM event would vanish from "Upcoming" at 9:01 PM.</item>
/// </list>
/// "Upcoming" means "not over yet", so an event that is happening right now is still listed.
/// </summary>
public static class EventTiming
{
    public const int DefaultHours = 3;

    public static DateTimeOffset EndOf(DateTimeOffset startsAt, DateTimeOffset? endsAt, bool isAllDay) =>
        isAllDay ? (endsAt ?? startsAt).AddHours(24) : endsAt ?? startsAt.AddHours(DefaultHours);

    /// <summary>Not over at <paramref name="now"/> (translated to SQL; keep in step with <see cref="EndOf"/>).</summary>
    public static IQueryable<Event> NotOver(this IQueryable<Event> events, DateTimeOffset now) =>
        events.Where(item => (item.IsAllDay
            ? (item.EndsAt ?? item.StartsAt).AddHours(24)
            : item.EndsAt ?? item.StartsAt.AddHours(DefaultHours)) > now);

    /// <summary>Over at <paramref name="now"/>: exactly the events <see cref="NotOver"/> leaves out.</summary>
    public static IQueryable<Event> Over(this IQueryable<Event> events, DateTimeOffset now) =>
        events.Where(item => (item.IsAllDay
            ? (item.EndsAt ?? item.StartsAt).AddHours(24)
            : item.EndsAt ?? item.StartsAt.AddHours(DefaultHours)) <= now);
}
