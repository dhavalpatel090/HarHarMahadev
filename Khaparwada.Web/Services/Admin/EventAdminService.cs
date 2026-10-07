using System.Globalization;
using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Events;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

/// <summary>The result of a save, and whether the event is already over (so the list opens on the right tab).</summary>
public sealed record EventSaveOutcome(OperationResult Result, bool IsOver);

public interface IEventAdminService
{
    Task<EventAdminListViewModel> ListAsync(EventListFilter filter, string? search, int? page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventPlaceOption>> GetPlacesAsync(CancellationToken cancellationToken = default);
    Task<EventInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>A new event's form filled from an existing one (yearly festivals): no dates, photo or web address.</summary>
    Task<EventInputModel?> GetCopyAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the event when input.Id is null, otherwise updates it.</summary>
    Task<EventSaveOutcome> SaveAsync(EventInputModel input, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the event leaves the website at once; the row stays recoverable.</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class EventAdminService(AppDbContext db, IImageUploadService images, IVillageClock clock) : IEventAdminService
{
    public const int PageSize = 25;
    private const string MediaFolder = "events";
    private const string ListRouteName = "admin-events";

    public async Task<EventAdminListViewModel> ListAsync(
        EventListFilter filter, string? search, int? page, CancellationToken cancellationToken = default)
    {
        search = AdminText.Optional(search);
        var now = clock.UtcNow;
        var query = db.Events.AsNoTracking();
        if (search is not null)
        {
            query = query.Where(e => e.Title.Contains(search) || (e.TitleGu != null && e.TitleGu.Contains(search)));
        }

        var upcoming = await query.NotOver(now).CountAsync(cancellationToken);
        var past = await query.Over(now).CountAsync(cancellationToken);
        var counts = new Dictionary<EventListFilter, int>
        {
            [EventListFilter.Upcoming] = upcoming,
            [EventListFilter.Past] = past,
            [EventListFilter.All] = upcoming + past
        };

        // Upcoming: soonest first; past and all: newest first.
        var ordered = filter switch
        {
            EventListFilter.Upcoming => query.NotOver(now).OrderBy(e => e.StartsAt),
            EventListFilter.Past => query.Over(now).OrderByDescending(e => e.StartsAt),
            _ => query.OrderByDescending(e => e.StartsAt)
        };

        var total = counts[filter];
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var rows = await ordered
            .ThenBy(e => e.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.TitleGu,
                e.StartsAt,
                e.EndsAt,
                e.IsAllDay,
                Location = e.Place != null ? e.Place.Name : e.LocationText,
                e.ImagePath,
                e.IsFeatured,
                e.IsPublished,
                IsPlaceholder = e.Title.StartsWith("[") || e.Summary.StartsWith("[")
            })
            .ToListAsync(cancellationToken);

        return new EventAdminListViewModel
        {
            Items = rows.Select(row => new EventListItem(
                row.Id, row.Title, row.TitleGu,
                clock.ToVillageTime(row.StartsAt), row.EndsAt is { } end ? clock.ToVillageTime(end) : null, row.IsAllDay,
                row.Location, row.ImagePath, row.IsFeatured, row.IsPublished, row.IsPlaceholder,
                EventTiming.EndOf(row.StartsAt, row.EndsAt, row.IsAllDay) <= now)).ToList(),
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
                    ["show"] = filter == EventListFilter.Upcoming ? null : filter.ToString().ToLowerInvariant(),
                    ["q"] = search
                }
            }
        };
    }

    public async Task<IReadOnlyList<EventPlaceOption>> GetPlacesAsync(CancellationToken cancellationToken = default) =>
        await db.Places.AsNoTracking()
            .OrderBy(place => place.Name)
            .Select(place => new EventPlaceOption(place.Id, place.Name, place.IsPublished && place.Category.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<EventInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var e = await db.Events.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (e is null)
        {
            return null;
        }

        // Back to what people typed: village dates and clock times. A timed event's end date is shown only when it
        // differs from the start date (an evening that ends after midnight, or several days).
        var start = clock.ToVillageTime(e.StartsAt).DateTime;
        DateTime? end = e.EndsAt is { } endsAt ? clock.ToVillageTime(endsAt).DateTime : null;
        return new EventInputModel
        {
            Id = e.Id,
            Title = e.Title,
            TitleGu = e.TitleGu,
            Slug = e.Slug,
            Summary = e.Summary,
            SummaryGu = e.SummaryGu,
            Description = e.Description,
            DescriptionGu = e.DescriptionGu,
            IsAllDay = e.IsAllDay,
            StartDate = DateOnly.FromDateTime(start),
            StartTime = e.IsAllDay ? null : TimeOnly.FromDateTime(start),
            EndDate = end is { } last && (e.IsAllDay || last.Date != start.Date) ? DateOnly.FromDateTime(last) : null,
            EndTime = end is { } endTime && !e.IsAllDay ? TimeOnly.FromDateTime(endTime) : null,
            PlaceId = e.PlaceId,
            LocationText = e.LocationText,
            LocationTextGu = e.LocationTextGu,
            CurrentImageUrl = MediaUrls.Image(e.ImagePath),
            IsFeatured = e.IsFeatured,
            IsPublished = e.IsPublished
        };
    }

    public async Task<EventInputModel?> GetCopyAsync(int id, CancellationToken cancellationToken = default)
    {
        var model = await GetAsync(id, cancellationToken);
        if (model is null)
        {
            return null;
        }

        // The photo is not copied: both events would share one file, and replacing it on one would delete it from
        // the other. Times of day are kept (a yearly Garba starts at 9 PM every year); the dates are not.
        model.CopiedFrom = model.Title;
        model.Id = null;
        model.Slug = null;
        model.StartDate = null;
        model.EndDate = null;
        model.CurrentImageUrl = null;
        return model;
    }

    public async Task<EventSaveOutcome> SaveAsync(EventInputModel input, CancellationToken cancellationToken = default)
    {
        if (input.StartDate is not { } startDate)
        {
            return Failed(nameof(input.StartDate), "Choose the date.");
        }

        if (!input.IsAllDay && input.StartTime is null)
        {
            return Failed(nameof(input.StartTime), "Enter the start time, or tick “All day”.");
        }

        // When: village date and time → exact moments (see EventTiming for what the end means).
        DateTimeOffset startsAt;
        DateTimeOffset? endsAt = null;
        if (input.IsAllDay)
        {
            startsAt = clock.FromVillageTime(startDate, TimeOnly.MinValue);
            if (input.EndDate is { } lastDay)
            {
                if (lastDay < startDate)
                {
                    return Failed(nameof(input.EndDate), "The last day cannot be before the first day.");
                }

                endsAt = lastDay > startDate ? clock.FromVillageTime(lastDay, TimeOnly.MinValue) : null;
            }
        }
        else
        {
            var startTime = input.StartTime!.Value;
            startsAt = clock.FromVillageTime(startDate, startTime);
            if (input.EndTime is { } endTime)
            {
                // Without a last day, an end time earlier than the start means after midnight: "9 PM – 12:30 AM".
                var endDate = input.EndDate ?? (endTime <= startTime ? startDate.AddDays(1) : startDate);
                endsAt = clock.FromVillageTime(endDate, endTime);
                if (endsAt <= startsAt)
                {
                    return Failed(nameof(input.EndTime), "The event must end after it starts.");
                }
            }
            else if (input.EndDate is not null)
            {
                return Failed(nameof(input.EndTime), "Enter the end time too, or leave the last day empty.");
            }
        }

        // Where: a venue from the Places list, or words, or both ("Hall behind the temple").
        var locationText = AdminText.Optional(input.LocationText);
        if (input.PlaceId is null && locationText is null)
        {
            return Failed(nameof(input.LocationText), "Choose a venue from the list, or write where it happens.");
        }

        if (input.PlaceId is { } placeId && !await db.Places.AnyAsync(p => p.Id == placeId, cancellationToken))
        {
            return Failed(nameof(input.PlaceId), "Choose a venue from the list.");
        }

        // The year goes into a new event's address, so next year's festival does not collide with this one.
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? DefaultSlug(input.Title, startDate.Year) : input.Slug.Trim();
        if (!SlugGenerator.IsValid(slug))
        {
            return Failed(nameof(input.Slug), "Enter a web address in English letters, e.g. navratri-garba-2026.");
        }

        if (await db.Events.AnyAsync(e => e.Slug == slug && e.Id != input.Id, cancellationToken))
        {
            return Failed(nameof(input.Slug), "Another event already uses this web address.");
        }

        Event? item = null;
        if (input.Id is { } id)
        {
            item = await db.Events.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (item is null)
            {
                return Failed(string.Empty, "This event no longer exists.");
            }
        }

        var image = await ImageEdit.PrepareAsync(images, input.Image, input.RemoveImage, MediaFolder, cancellationToken);
        if (image.Failed)
        {
            return Failed(nameof(input.Image), image.Error!);
        }

        if (item is null)
        {
            item = new Event();
            db.Events.Add(item);
        }

        item.Title = input.Title.Trim();
        item.TitleGu = AdminText.Optional(input.TitleGu);
        item.Slug = slug;
        item.Summary = input.Summary.Trim();
        item.SummaryGu = AdminText.Optional(input.SummaryGu);
        item.Description = AdminText.Optional(input.Description);
        item.DescriptionGu = AdminText.Optional(input.DescriptionGu);
        item.StartsAt = startsAt;
        item.EndsAt = endsAt;
        item.IsAllDay = input.IsAllDay;
        item.PlaceId = input.PlaceId;
        item.LocationText = locationText;
        item.LocationTextGu = AdminText.Optional(input.LocationTextGu);
        item.ImagePath = image.Apply(item.ImagePath);
        item.IsFeatured = input.IsFeatured;
        item.IsPublished = input.IsPublished;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two people saving the same web address at the same moment: the unique index catches it.
            image.SaveFailed();
            return Failed(nameof(input.Slug), "Another event already uses this web address.");
        }
        catch
        {
            image.SaveFailed();
            throw;
        }

        image.SaveSucceeded();
        return new EventSaveOutcome(OperationResult.Success(), EventTiming.EndOf(startsAt, endsAt, input.IsAllDay) <= clock.UtcNow);
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await db.Events.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (item is null)
        {
            return OperationResult.Failure(string.Empty, "This event no longer exists.");
        }

        db.Events.Remove(item); // becomes a soft delete (AuditSaveChangesInterceptor); the photo stays for a restore
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    /// <summary>"Navratri Garba Night" in 2026 → "navratri-garba-night-2026"; a title with the year keeps it once.</summary>
    public static string DefaultSlug(string title, int year)
    {
        var slug = SlugGenerator.Create(title);
        var yearText = year.ToString(CultureInfo.InvariantCulture);
        return slug.Length == 0 || slug.Split('-').Contains(yearText) ? slug : SlugGenerator.Create($"{slug} {yearText}");
    }

    private static EventSaveOutcome Failed(string field, string message) =>
        new(OperationResult.Failure(field, message), false);
}
