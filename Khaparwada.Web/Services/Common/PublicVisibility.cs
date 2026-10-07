using System.Linq.Expressions;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Events;

namespace Khaparwada.Web.Services.Common;

/// <summary>
/// The rules for what the public site may show, in one place. Every public query goes through these, so
/// "is this visible?" is answered identically on every page and can be reviewed here in one read.
/// (Soft-deleted rows are already removed by the query filters in Data/Configurations.)
/// </summary>
public static class PublicVisibility
{
    /// <summary>
    /// An approved photo in a published album whose category is switched on. Also usable inside projections, e.g.
    /// <c>album.Images.AsQueryable().Count(GalleryImageIsVisible)</c>.
    /// </summary>
    public static readonly Expression<Func<GalleryImage, bool>> GalleryImageIsVisible =
        image => image.Status == ModerationStatus.Published && image.Album.IsPublished && image.Album.Category.IsActive;

    /// <summary>
    /// Published, in a category that is switched on. Switching a category off hides its places too; otherwise a
    /// place would show a category chip and filter link for a category visitors cannot see.
    /// </summary>
    public static IQueryable<Place> Visible(this IQueryable<Place> places) =>
        places.Where(place => place.IsPublished && place.Category.IsActive);

    public static IQueryable<Event> Visible(this IQueryable<Event> events) =>
        events.Where(item => item.IsPublished);

    /// <summary>Published events that are not over yet (the rule for "over" is in <see cref="EventTiming"/>).</summary>
    public static IQueryable<Event> Upcoming(this IQueryable<Event> events, DateTimeOffset now) =>
        events.Visible().NotOver(now);

    /// <summary>Published events that are over: together with <see cref="Upcoming"/>, every published event once.</summary>
    public static IQueryable<Event> Past(this IQueryable<Event> events, DateTimeOffset now) =>
        events.Visible().Over(now);

    /// <summary>Published, already started and not yet expired.</summary>
    public static IQueryable<Notice> Active(this IQueryable<Notice> notices, DateTimeOffset now) =>
        notices.Where(notice => notice.IsPublished
                                && notice.PublishedAt <= now
                                && (notice.ExpiresAt == null || notice.ExpiresAt > now));

    /// <summary>Published by an admin, with the person's consent on record, in a category that is switched on.</summary>
    public static IQueryable<Person> Visible(this IQueryable<Person> people) =>
        people.Where(person => person.Status == ModerationStatus.Published && person.HasConsent && person.Category.IsActive);

    /// <summary>
    /// Published, in a category that is switched on, with at least one approved photo: an album with nothing to
    /// show (all its photos still waiting for approval) would be a dead end for visitors.
    /// </summary>
    public static IQueryable<GalleryAlbum> Visible(this IQueryable<GalleryAlbum> albums) =>
        albums.Where(album => album.IsPublished
                              && album.Category.IsActive
                              && album.Images.Any(image => image.Status == ModerationStatus.Published));

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
