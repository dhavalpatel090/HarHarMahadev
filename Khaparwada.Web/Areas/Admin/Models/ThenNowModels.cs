using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class ThenNowInputModel
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter a title, e.g. the name of the place.")]
    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string? TitleGu { get; set; }

    [StringLength(FieldLengths.Caption)]
    [Display(Name = "The story behind the photos")]
    public string? Caption { get; set; }

    [StringLength(FieldLengths.Caption)]
    [Display(Name = "The story behind the photos")]
    public string? CaptionGu { get; set; }

    [Required(ErrorMessage = "Say when the old photo was taken, e.g. 1985 or 1980s.")]
    [StringLength(FieldLengths.Label)]
    [Display(Name = "Label of the old photo")]
    public string ThenLabel { get; set; } = string.Empty;

    [StringLength(FieldLengths.Label)]
    [Display(Name = "Label of the old photo")]
    public string? ThenLabelGu { get; set; }

    [Required(ErrorMessage = "Enter a label for the new photo, e.g. Today.")]
    [StringLength(FieldLengths.Label)]
    [Display(Name = "Label of the new photo")]
    public string NowLabel { get; set; } = "Today";

    [StringLength(FieldLengths.Label)]
    [Display(Name = "Label of the new photo")]
    public string? NowLabelGu { get; set; } = "આજે";

    [Display(Name = "Old photo")]
    public IFormFile? ThenImage { get; set; }

    public bool RemoveThenImage { get; set; }

    [BindNever]
    public string? CurrentThenImageUrl { get; set; }

    [Display(Name = "New photo")]
    public IFormFile? NowImage { get; set; }

    public bool RemoveNowImage { get; set; }

    [BindNever]
    public string? CurrentNowImageUrl { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Order on the page")]
    public int SortOrder { get; set; }

    [Display(Name = "Show on the website")]
    public bool IsPublished { get; set; } = true;

    public bool IsNew => Id is null;
}

public sealed record ThenNowListItem(
    int Id, string Title, string? TitleGu, string ThenLabel, string NowLabel, string? ThenImagePath, string? NowImagePath,
    int SortOrder, bool IsPublished);
