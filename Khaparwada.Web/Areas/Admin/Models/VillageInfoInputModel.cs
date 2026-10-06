using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>The Village info form. Lengths come from FieldLengths, so they always match the database.</summary>
public sealed class VillageInfoInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Enter the village name.")]
    [StringLength(FieldLengths.Name)]
    [Display(Name = "Village name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(FieldLengths.Name)]
    [Display(Name = "Village name")]
    public string? NameGu { get; set; }

    [Required(ErrorMessage = "Write a short introduction.")]
    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Short introduction")]
    public string ShortIntro { get; set; } = string.Empty;

    [StringLength(FieldLengths.Intro)]
    [Display(Name = "Short introduction")]
    public string? ShortIntroGu { get; set; }

    [Range(0, 10_000_000)]
    [Display(Name = "Population")]
    public int? Population { get; set; }

    [Range(0, 10_000_000)]
    [Display(Name = "Families (households)")]
    public int? Households { get; set; }

    [Range(0.0, 100_000.0)]
    [Display(Name = "Area in km²")]
    public decimal? AreaSqKm { get; set; }

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Source of these figures")]
    public string? StatsSource { get; set; }

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Source of these figures")]
    public string? StatsSourceGu { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "Taluka")]
    public string? Taluka { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "Taluka")]
    public string? TalukaGu { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "District")]
    public string? District { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "District")]
    public string? DistrictGu { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "State")]
    public string? State { get; set; }

    [StringLength(FieldLengths.Label)]
    [Display(Name = "State")]
    public string? StateGu { get; set; }

    [RegularExpression("^[1-9][0-9]{5}$", ErrorMessage = "A PIN code has 6 digits and does not start with 0.")]
    [Display(Name = "PIN code")]
    public string? PinCode { get; set; }

    [Range(-90.0, 90.0)]
    [Display(Name = "Latitude")]
    public decimal? Latitude { get; set; }

    [Range(-180.0, 180.0)]
    [Display(Name = "Longitude")]
    public decimal? Longitude { get; set; }

    [Range(5, 19)]
    [Display(Name = "Map zoom")]
    public int MapZoom { get; set; } = 15;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude is null != Longitude is null)
        {
            yield return new ValidationResult(
                "Enter both latitude and longitude, or leave both empty.",
                [nameof(Latitude), nameof(Longitude)]);
        }
    }
}
