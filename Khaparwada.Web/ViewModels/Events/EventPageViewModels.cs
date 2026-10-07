using System.Globalization;
using Khaparwada.Web.ViewModels.Gallery;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.Events;

/// <summary>The two tabs of /events: "Upcoming" (default) and "Past" (?show=past).</summary>
public enum EventListView
{
    Upcoming,
    Past
}

/// <summary>/events: one tab, one page of events (soonest first; past events newest first).</summary>
public sealed class EventListViewModel
{
    public EventListView View { get; init; }
    public required IReadOnlyList<EventCardViewModel> Events { get; init; }
    public int UpcomingCount { get; init; }
    public int PastCount { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

public enum EventState
{
    Upcoming,
    Now,
    Past
}

/// <summary>/events/{slug}. Times are in village time.</summary>
public sealed class EventDetailsViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public string? Description { get; init; }
    public required DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }
    public bool IsAllDay { get; init; }
    public EventState State { get; init; }
    public string? ImagePath { get; init; }

    /// <summary>The venue from the Places list (its name even when its page is hidden).</summary>
    public string? VenueName { get; init; }

    /// <summary>Set only when the venue's page is public, so the link never leads to a "not found" page.</summary>
    public string? VenueSlug { get; init; }
    public string? VenueAddress { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    /// <summary>Where it happens, in words: on its own, or as a note to the venue ("Hall behind the temple").</summary>
    public string? LocationText { get; init; }

    public required IReadOnlyList<EventCardViewModel> MoreEvents { get; init; }

    /// <summary>Photo albums from this event (Gallery), newest first.</summary>
    public IReadOnlyList<AlbumCardViewModel> Albums { get; init; } = [];

    public string DateText => EventSchedule.DateText(StartsAt, EndsAt, IsAllDay, CultureInfo.CurrentCulture);
    public string? TimeText => EventSchedule.TimeText(StartsAt, EndsAt, IsAllDay, CultureInfo.CurrentCulture);
    public string? DirectionsUrl => MapLinks.Directions(Latitude, Longitude);
    public bool HasLocation => VenueName is not null || LocationText is not null;
}

/// <summary>An event as a calendar file ("Add to calendar").</summary>
public sealed record EventCalendarFile(string FileName, string Content);
