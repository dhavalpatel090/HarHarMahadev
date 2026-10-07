using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Infrastructure.Validation;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class PlaceInputModel : IValidatableObject
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Enter the name of the place.")]
    [StringLength(FieldLengths.Name)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(FieldLengths.Name)]
    [Display(Name = "Name")]
    public string? NameGu { get; set; }

    [StringLength(FieldLengths.Slug)]
    [RegularExpression(SlugGenerator.Pattern, ErrorMessage = "Use lowercase English letters, numbers and single hyphens, e.g. village-lake.")]
    [Display(Name = "Web address")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Write one or two sentences about the place.")]
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

    [Display(Name = "Main photo")]
    public IFormFile? Image { get; set; }

    public bool RemoveImage { get; set; }

    [BindNever]
    public string? CurrentImageUrl { get; set; }

    [StringLength(FieldLengths.Address)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(FieldLengths.Address)]
    [Display(Name = "Address")]
    public string? AddressGu { get; set; }

    [Range(-90.0, 90.0)]
    [Display(Name = "Latitude")]
    public decimal? Latitude { get; set; }

    [Range(-180.0, 180.0)]
    [Display(Name = "Longitude")]
    public decimal? Longitude { get; set; }

    [RegularExpression(@"^[0-9+\-\s()]{6,20}$", ErrorMessage = "Use digits, spaces, + and - only.")]
    [StringLength(FieldLengths.Phone)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(FieldLengths.Email)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [HttpUrl, StringLength(FieldLengths.Url)]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [StringLength(FieldLengths.Timings)]
    [Display(Name = "Timings")]
    public string? Timings { get; set; }

    [StringLength(FieldLengths.Timings)]
    [Display(Name = "Timings")]
    public string? TimingsGu { get; set; }

    [Display(Name = "Featured: show it on the Home page")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Show on the website")]
    public bool IsPublished { get; set; } = true;

    /// <summary>For the category list; filled by the controller, never bound.</summary>
    [BindNever]
    public IReadOnlyList<PlaceCategoryOption> Categories { get; set; } = [];

    [BindNever]
    public int PhotoCount { get; set; }

    public bool IsNew => Id is null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude is null != Longitude is null)
        {
            yield return new ValidationResult(
                "Enter both latitude and longitude, or leave both empty.", [nameof(Latitude), nameof(Longitude)]);
        }
    }
}

public sealed record PlaceCategoryOption(int Id, string Name, bool IsActive);

public sealed record PlaceListItem(
    int Id, string Name, string? NameGu, string Slug, string CategoryName, string CategoryIcon, string CategoryColor,
    string? ImagePath, bool HasLocation, bool IsFeatured, bool IsPublished, bool IsPlaceholder, int PhotoCount);

public sealed class PlaceAdminListViewModel
{
    public required IReadOnlyList<PlaceListItem> Items { get; init; }
    public required IReadOnlyList<PlaceCategoryOption> Categories { get; init; }
    public int? CategoryId { get; init; }
    public string? Search { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>The Photos page of one place: captions, order and removal of its gallery photos.</summary>
public sealed class PlacePhotosForm
{
    [BindNever]
    public int PlaceId { get; set; }

    [BindNever]
    public string PlaceName { get; set; } = string.Empty;

    [BindNever]
    public string PlaceSlug { get; set; } = string.Empty;

    public List<PlacePhotoRow> Photos { get; set; } = [];
}

public sealed class PlacePhotoRow
{
    public int Id { get; set; }

    /// <summary>Shown only; the service never takes a file path from the form.</summary>
    [BindNever]
    public string ImagePath { get; set; } = string.Empty;

    [StringLength(FieldLengths.Caption)]
    [Display(Name = "Caption")]
    public string? Caption { get; set; }

    [StringLength(FieldLengths.Caption)]
    [Display(Name = "Caption")]
    public string? CaptionGu { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Order")]
    public int SortOrder { get; set; }

    [Display(Name = "Remove")]
    public bool Remove { get; set; }
}
