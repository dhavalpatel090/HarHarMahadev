using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.ViewModels.Events;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>
/// The event form. Dates and times are what people in the village mean ("17 Oct, 9:00 PM"); the service turns
/// them into exact moments. All-day events have no times; their last day is optional.
/// </summary>
public sealed class EventInputModel
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the name of the event.")]
    [StringLength(FieldLengths.Title)]
    [Display(Name = "Name of the event")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Name of the event")]
    public string? TitleGu { get; set; }

    [StringLength(FieldLengths.Slug)]
    [RegularExpression(SlugGenerator.Pattern, ErrorMessage = "Use lowercase English letters, numbers and single hyphens, e.g. navratri-garba-2026.")]
    [Display(Name = "Web address")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Write one or two sentences about the event.")]
    [StringLength(FieldLengths.Summary)]
    [Display(Name = "Short description")]
    public string Summary { get; set; } = string.Empty;

    [StringLength(FieldLengths.Summary)]
    [Display(Name = "Short description")]
    public string? SummaryGu { get; set; }

    [StringLength(FieldLengths.LongText)]
    [Display(Name = "Full description")]
    public string? Description { get; set; }

    [StringLength(FieldLengths.LongText)]
    [Display(Name = "Full description")]
    public string? DescriptionGu { get; set; }

    [Required(ErrorMessage = "Choose the date.")]
    [Display(Name = "Date")]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "Starts at")]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "Ends at")]
    public TimeOnly? EndTime { get; set; }

    [Display(Name = "Last day")]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "All day (no start time)")]
    public bool IsAllDay { get; set; }

    [Display(Name = "Venue from the Places list")]
    public int? PlaceId { get; set; }

    [StringLength(FieldLengths.Address)]
    [Display(Name = "Where, in words")]
    public string? LocationText { get; set; }

    [StringLength(FieldLengths.Address)]
    [Display(Name = "Where, in words")]
    public string? LocationTextGu { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? Image { get; set; }

    public bool RemoveImage { get; set; }

    [BindNever]
    public string? CurrentImageUrl { get; set; }

    [Display(Name = "Featured: always among the events on the Home page while it is upcoming")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Shown on the website")]
    public bool IsPublished { get; set; } = true;

    [BindNever]
    public IReadOnlyList<EventPlaceOption> Places { get; set; } = [];

    /// <summary>The event this form was copied from ("Copy" in the list), for the note at the top.</summary>
    [BindNever]
    public string? CopiedFrom { get; set; }

    public bool IsNew => Id is null;
}

/// <summary>A venue in the form's list; hidden places can be chosen, but their page is not linked.</summary>
public sealed record EventPlaceOption(int Id, string Name, bool IsPublic);

/// <summary>The tabs of the admin Events list (?show=past).</summary>
public enum EventListFilter
{
    Upcoming,
    Past,
    All
}

/// <summary>One row of the admin list; times in village time.</summary>
public sealed record EventListItem(
    int Id, string Title, string? TitleGu, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool IsAllDay,
    string? Location, string? ImagePath, bool IsFeatured, bool IsPublished, bool IsPlaceholder, bool IsOver)
{
    public string DateText => EventSchedule.DateText(StartsAt, EndsAt, IsAllDay, CultureInfo.CurrentCulture);
    public string TimeText => EventSchedule.TimeText(StartsAt, EndsAt, IsAllDay, CultureInfo.CurrentCulture) ?? "All day";
}

public sealed class EventAdminListViewModel
{
    public required IReadOnlyList<EventListItem> Items { get; init; }
    public required IReadOnlyDictionary<EventListFilter, int> Counts { get; init; }
    public EventListFilter Filter { get; init; }
    public string? Search { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}
