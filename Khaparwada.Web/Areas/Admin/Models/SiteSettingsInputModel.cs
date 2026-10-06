using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Validation;
using Khaparwada.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class SiteSettingsInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Enter the website name.")]
    [StringLength(FieldLengths.Name)]
    [Display(Name = "Website name")]
    public string SiteName { get; set; } = string.Empty;

    [StringLength(FieldLengths.Name)]
    [Display(Name = "Website name")]
    public string? SiteNameGu { get; set; }

    [Required(ErrorMessage = "Enter the tagline.")]
    [StringLength(FieldLengths.Title)]
    [Display(Name = "Tagline")]
    public string Tagline { get; set; } = string.Empty;

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Tagline")]
    public string? TaglineGu { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(FieldLengths.Email)]
    [Display(Name = "Office email")]
    public string? OfficeEmail { get; set; }

    [RegularExpression(@"^[0-9+\-\s()]{6,20}$", ErrorMessage = "Use digits, spaces, + and - only.")]
    [StringLength(FieldLengths.Phone)]
    [Display(Name = "Office phone")]
    public string? OfficePhone { get; set; }

    [Display(Name = "Show the office phone number on the website")]
    public bool ShowOfficePhone { get; set; }

    [StringLength(FieldLengths.Address)]
    [Display(Name = "Office address")]
    public string? OfficeAddress { get; set; }

    [StringLength(FieldLengths.Address)]
    [Display(Name = "Office address")]
    public string? OfficeAddressGu { get; set; }

    [HttpUrl, StringLength(FieldLengths.Url)]
    [Display(Name = "Facebook page")]
    public string? FacebookUrl { get; set; }

    [HttpUrl, StringLength(FieldLengths.Url)]
    [Display(Name = "Instagram")]
    public string? InstagramUrl { get; set; }

    [HttpUrl, StringLength(FieldLengths.Url)]
    [Display(Name = "YouTube channel")]
    public string? YouTubeUrl { get; set; }

    [HttpUrl, StringLength(FieldLengths.Url)]
    [Display(Name = "WhatsApp channel or group link")]
    public string? WhatsAppUrl { get; set; }

    [StringLength(FieldLengths.Summary)]
    [Display(Name = "Description for search engines and link previews")]
    public string? DefaultMetaDescription { get; set; }

    [StringLength(FieldLengths.Summary)]
    [Display(Name = "Description for search engines and link previews")]
    public string? DefaultMetaDescriptionGu { get; set; }

    [Display(Name = "New photo")]
    public IFormFile? HeroImage { get; set; }

    [Display(Name = "Remove the photo and show the illustration again")]
    public bool RemoveHeroImage { get; set; }

    [Display(Name = "New video")]
    public IFormFile? HeroVideo { get; set; }

    [Display(Name = "Remove the video")]
    public bool RemoveHeroVideo { get; set; }

    /// <summary>Shown on the form only; never read from a submitted form.</summary>
    [BindNever]
    public string? CurrentHeroImageUrl { get; set; }

    [BindNever]
    public string? CurrentHeroVideoUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ShowOfficePhone && string.IsNullOrWhiteSpace(OfficePhone))
        {
            yield return new ValidationResult("Enter the phone number, or untick “show on the website”.", [nameof(OfficePhone)]);
        }
    }
}
