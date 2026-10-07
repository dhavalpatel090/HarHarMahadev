using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class TimelineEntryInputModel
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    /// <summary>Puts the entries in order. Nullable so a new form starts empty instead of with "0".</summary>
    [Required(ErrorMessage = "Enter the year. It puts the entries in order.")]
    [Range(1, 2200, ErrorMessage = "Enter a year between 1 and 2200.")]
    [Display(Name = "Year")]
    public int? SortYear { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "Year as shown")]
    public string? YearLabel { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "Year as shown")]
    public string? YearLabelGu { get; set; }

    [Required(ErrorMessage = "Enter a title.")]
    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string? TitleGu { get; set; }

    [Required(ErrorMessage = "Write what happened.")]
    [StringLength(FieldLengths.LongText)]
    [Display(Name = "What happened")]
    public string Description { get; set; } = string.Empty;

    [StringLength(FieldLengths.LongText)]
    [Display(Name = "What happened")]
    public string? DescriptionGu { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? Image { get; set; }

    public bool RemoveImage { get; set; }

    [BindNever]
    public string? CurrentImageUrl { get; set; }

    [Display(Name = "Milestone: also show it on the Home page and the About page")]
    public bool IsHighlighted { get; set; }

    [Display(Name = "Show on the website")]
    public bool IsPublished { get; set; } = true;

    public bool IsNew => Id is null;
}

public sealed record TimelineListItem(
    int Id, int SortYear, string YearLabel, string Title, string? TitleGu, string? ImagePath,
    bool IsHighlighted, bool IsPublished, bool IsPlaceholder);
