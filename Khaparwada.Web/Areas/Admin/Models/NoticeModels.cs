using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>
/// The notice form. "Show from" is a village date and time (default: now); "Last day" is the last date it is
/// on the board, so a notice ends at midnight after that day.
/// </summary>
public sealed class NoticeInputModel
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the notice's title.")]
    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string? TitleGu { get; set; }

    [StringLength(FieldLengths.Slug)]
    [RegularExpression(SlugGenerator.Pattern, ErrorMessage = "Use lowercase English letters, numbers and single hyphens, e.g. water-supply-update.")]
    [Display(Name = "Web address")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Write the notice.")]
    [StringLength(FieldLengths.LongText)]
    [Display(Name = "The notice")]
    public string Description { get; set; } = string.Empty;

    [StringLength(FieldLengths.LongText)]
    [Display(Name = "The notice")]
    public string? DescriptionGu { get; set; }

    [Display(Name = "Kind")]
    public NoticeCategory Category { get; set; } = NoticeCategory.General;

    [Display(Name = "Priority")]
    public NoticePriority Priority { get; set; } = NoticePriority.Normal;

    [Required(ErrorMessage = "Choose the date.")]
    [Display(Name = "Show from")]
    public DateOnly? PublishDate { get; set; }

    [Required(ErrorMessage = "Enter the time.")]
    [Display(Name = "At")]
    public TimeOnly? PublishTime { get; set; }

    [Display(Name = "Last day")]
    public DateOnly? LastDay { get; set; }

    [Display(Name = "Photo or poster")]
    public IFormFile? Image { get; set; }

    public bool RemoveImage { get; set; }

    [BindNever]
    public string? CurrentImageUrl { get; set; }

    [Display(Name = "PDF attachment")]
    public IFormFile? Attachment { get; set; }

    [Display(Name = "Remove the attachment")]
    public bool RemoveAttachment { get; set; }

    [BindNever]
    public string? CurrentAttachmentName { get; set; }

    [Display(Name = "Shown on the website")]
    public bool IsPublished { get; set; } = true;

    /// <summary>Set by a successful save: where the notice is now, so the list opens on that tab.</summary>
    [BindNever]
    public NoticeState? SavedState { get; set; }

    public bool IsNew => Id is null;
}

/// <summary>The tabs of the admin notice list (?show=ended).</summary>
public enum NoticeListFilter
{
    Active,
    Scheduled,
    Ended,
    Hidden,
    All
}

/// <summary>Where a notice is now, worked out from its dates (never stored).</summary>
public enum NoticeState
{
    Active,
    Scheduled,
    Ended,
    Hidden
}

/// <summary>One row of the admin list; times in village time.</summary>
public sealed record NoticeListItem(
    int Id, string Title, string? TitleGu, NoticeCategory Category, NoticePriority Priority,
    DateTimeOffset PublishedAt, DateOnly? LastDay, NoticeState State, bool HasAttachment, bool IsPlaceholder);

public sealed class NoticeAdminListViewModel
{
    public required IReadOnlyList<NoticeListItem> Items { get; init; }
    public required IReadOnlyDictionary<NoticeListFilter, int> Counts { get; init; }
    public NoticeListFilter Filter { get; init; }
    public string? Search { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}
