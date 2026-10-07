using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class PersonInputModel
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Enter the person's name.")]
    [StringLength(FieldLengths.Name)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(FieldLengths.Name)]
    [Display(Name = "Full name")]
    public string? FullNameGu { get; set; }

    [StringLength(FieldLengths.Slug)]
    [RegularExpression(SlugGenerator.Pattern, ErrorMessage = "Use lowercase English letters, numbers and single hyphens, e.g. ramesh-patel.")]
    [Display(Name = "Web address")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Enter their work or role, e.g. Teacher.")]
    [StringLength(FieldLengths.Label)]
    [Display(Name = "Work or role")]
    public string Profession { get; set; } = string.Empty;

    [StringLength(FieldLengths.Label)]
    [Display(Name = "Work or role")]
    public string? ProfessionGu { get; set; }

    [Required(ErrorMessage = "Write a short biography.")]
    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Short biography")]
    public string ShortBio { get; set; } = string.Empty;

    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Short biography")]
    public string? ShortBioGu { get; set; }

    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Achievement")]
    public string? Achievement { get; set; }

    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Achievement")]
    public string? AchievementGu { get; set; }

    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Contribution to the village")]
    public string? Contribution { get; set; }

    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Contribution to the village")]
    public string? ContributionGu { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? Photo { get; set; }

    public bool RemovePhoto { get; set; }

    [BindNever]
    public string? CurrentPhotoUrl { get; set; }

    [Display(Name = "The person (or, for a child or someone who has died, their family) has agreed to this profile being on the website")]
    public bool HasConsent { get; set; }

    [StringLength(FieldLengths.ConsentNote)]
    [Display(Name = "How consent was given")]
    public string? ConsentNote { get; set; }

    [Display(Name = "Featured: show on the Home page")]
    public bool IsFeatured { get; set; }

    /// <summary>The review decision. Only read for Admins; the service ignores it for Editors.</summary>
    [Display(Name = "Decision")]
    public ModerationStatus? Status { get; set; }

    [StringLength(FieldLengths.ReviewNote)]
    [Display(Name = "Note for the team (not shown on the website)")]
    public string? ReviewNote { get; set; }

    // Shown on the form only.
    [BindNever]
    public ModerationStatus CurrentStatus { get; set; } = ModerationStatus.Pending;

    [BindNever]
    public DateTimeOffset? ConsentRecordedAt { get; set; }

    [BindNever]
    public string? ReviewedBy { get; set; }

    [BindNever]
    public DateTimeOffset? ReviewedAt { get; set; }

    [BindNever]
    public IReadOnlyList<PlaceCategoryOption> Categories { get; set; } = [];

    public bool IsNew => Id is null;
}

/// <summary>The status tabs of the People list (?status=review).</summary>
public enum PersonListFilter
{
    All,
    Review,
    Published,
    Hidden,
    Rejected
}

public sealed record PersonListItem(
    int Id, string FullName, string? FullNameGu, string Profession, string CategoryName, string? PhotoPath,
    bool HasConsent, bool IsFeatured, ModerationStatus Status, bool IsPlaceholder)
{
    public string Initials => TextHelpers.Initials(FullName);
}

public sealed class PersonAdminListViewModel
{
    public required IReadOnlyList<PersonListItem> Items { get; init; }
    public required IReadOnlyList<PlaceCategoryOption> Categories { get; init; }
    public required IReadOnlyDictionary<PersonListFilter, int> Counts { get; init; }
    public PersonListFilter Filter { get; init; }
    public int? CategoryId { get; init; }
    public string? Search { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>Plain words for the review states, used in lists and on the form.</summary>
public static class ReviewStates
{
    public static string Label(ModerationStatus status) => status switch
    {
        ModerationStatus.Pending => "Waiting for review",
        ModerationStatus.UnderReview => "Being checked",
        ModerationStatus.Approved => "Approved, hidden",
        ModerationStatus.Published => "Shown",
        _ => "Sent back"
    };

    public static string CssClass(ModerationStatus status) => status switch
    {
        ModerationStatus.Published => "is-on",
        ModerationStatus.Pending or ModerationStatus.UnderReview => "is-todo",
        ModerationStatus.Rejected => "is-off",
        _ => string.Empty
    };
}
