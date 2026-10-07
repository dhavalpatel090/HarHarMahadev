using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class AboutSectionInputModel
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter a title.")]
    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Title")]
    public string? TitleGu { get; set; }

    [StringLength(FieldLengths.Slug)]
    [RegularExpression(SlugGenerator.Pattern, ErrorMessage = "Use lowercase English letters, numbers and single hyphens, e.g. village-life.")]
    [Display(Name = "Web address")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Write the text of this section.")]
    [StringLength(FieldLengths.LongText)]
    [Display(Name = "Text")]
    public string Body { get; set; } = string.Empty;

    [StringLength(FieldLengths.LongText)]
    [Display(Name = "Text")]
    public string? BodyGu { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? Image { get; set; }

    public bool RemoveImage { get; set; }

    /// <summary>Shown on the form only; never read from a submitted form.</summary>
    [BindNever]
    public string? CurrentImageUrl { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Order on the page")]
    public int SortOrder { get; set; }

    [Display(Name = "Show on the website")]
    public bool IsPublished { get; set; } = true;

    public bool IsNew => Id is null;
}

public sealed record AboutSectionListItem(
    int Id, string Title, string? TitleGu, string Slug, string? ImagePath, int SortOrder, bool IsPublished,
    bool IsPlaceholder, DateTimeOffset UpdatedAt);
