using System.Linq.Expressions;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Models.Enums;

namespace Khaparwada.Web.Services.Common;

/// <summary>
/// The rules for what the public site may show, in one place. Every public query goes through these, so
/// "is this visible?" is answered identically on every page and can be reviewed here in one read.
/// (Soft-deleted rows are already removed by the query filters in Data/Configurations.)
/// </summary>
public static class PublicVisibility
{
    /// <summary>Also usable inside projections, e.g. <c>album.Images.AsQueryable().Count(GalleryImageIsVisible)</c>.</summary>
    public static readonly Expression<Func<GalleryImage, bool>> GalleryImageIsVisible =
        image => image.Status == ModerationStatus.Published && image.Album.IsPublished;

    public static IQueryable<Place> Visible(this IQueryable<Place> places) =>
        places.Where(place => place.IsPublished);

    public static IQueryable<Event> Visible(this IQueryable<Event> events) =>
        events.Where(item => item.IsPublished);

    /// <summary>
    /// Events that have not finished yet. Without an end time, a timed event counts as running for 3 hours
    /// and an all-day event for the whole day; otherwise a 9 PM event would vanish at 9:01 PM.
    /// </summary>
    public static IQueryable<Event> Upcoming(this IQueryable<Event> events, DateTimeOffset now) =>
        events.Visible().Where(item => (item.EndsAt ?? item.StartsAt.AddHours(item.IsAllDay ? 24 : 3)) >= now);

    /// <summary>Published, already started and not yet expired.</summary>
    public static IQueryable<Notice> Active(this IQueryable<Notice> notices, DateTimeOffset now) =>
        notices.Where(notice => notice.IsPublished
                                && notice.PublishedAt <= now
                                && (notice.ExpiresAt == null || notice.ExpiresAt > now));

    /// <summary>Approved and published by an admin, with the person's consent on record.</summary>
    public static IQueryable<Person> Visible(this IQueryable<Person> people) =>
        people.Where(person => person.Status == ModerationStatus.Published && person.HasConsent);

    public static IQueryable<GalleryAlbum> Visible(this IQueryable<GalleryAlbum> albums) =>
        albums.Where(album => album.IsPublished);

    public static IQueryable<GalleryImage> Visible(this IQueryable<GalleryImage> images) =>
        images.Where(GalleryImageIsVisible);

    /// <summary>Published by an admin, with the submitter's consent on record.</summary>
    public static IQueryable<MemorySubmission> Visible(this IQueryable<MemorySubmission> memories) =>
        memories.Where(memory => memory.Status == ModerationStatus.Published && memory.ConsentToPublish);

    public static IQueryable<HistoryEntry> Visible(this IQueryable<HistoryEntry> entries) =>
        entries.Where(entry => entry.IsPublished);

    public static IQueryable<AboutSection> Visible(this IQueryable<AboutSection> sections) =>
        sections.Where(section => section.IsPublished);

    public static IQueryable<ThenNowComparison> Visible(this IQueryable<ThenNowComparison> comparisons) =>
        comparisons.Where(comparison => comparison.IsPublished);
}
