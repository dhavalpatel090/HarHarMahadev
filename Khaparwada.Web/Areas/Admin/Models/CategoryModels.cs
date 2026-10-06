using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>The three category lists. The value appears in admin URLs: /admin/categories/places.</summary>
public enum CategoryKind
{
    Places,
    People,
    Gallery
}

public static class CategoryKinds
{
    public static IReadOnlyList<CategoryKind> All { get; } = [CategoryKind.Places, CategoryKind.People, CategoryKind.Gallery];

    /// <summary>The kind as it appears in admin URLs: "places", "people", "gallery".</summary>
    public static string RouteValue(CategoryKind kind) => kind.ToString().ToLowerInvariant();

    /// <summary>"place", "people", "gallery": as in "Add a place category".</summary>
    public static string Noun(CategoryKind kind) => kind switch
    {
        CategoryKind.Places => "place",
        CategoryKind.People => "people",
        _ => "gallery"
    };

    public static string Title(CategoryKind kind) => kind switch
    {
        CategoryKind.Places => "Place categories",
        CategoryKind.People => "People categories",
        _ => "Gallery categories"
    };

    /// <summary>"1 place", "3 places", "0 albums".</summary>
    public static string CountOf(CategoryKind kind, int count) => (kind, count) switch
    {
        (CategoryKind.Places, 1) => "1 place",
        (CategoryKind.Places, _) => $"{count} places",
        (CategoryKind.People, 1) => "1 profile",
        (CategoryKind.People, _) => $"{count} profiles",
        (_, 1) => "1 album",
        _ => $"{count} albums"
    };
}

public sealed class CategoryInputModel
{
    /// <summary>From the URL, never from the form.</summary>
    [BindNever]
    public CategoryKind Kind { get; set; }

    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter a name.")]
    [StringLength(FieldLengths.Name)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(FieldLengths.Name)]
    [Display(Name = "Name")]
    public string? NameGu { get; set; }

    [StringLength(FieldLengths.Slug)]
    [RegularExpression(SlugGenerator.Pattern, ErrorMessage = "Use lowercase English letters, numbers and single hyphens, e.g. water-places.")]
    [Display(Name = "Web address")]
    public string? Slug { get; set; }

    /// <summary>Icon name from the sprite (places and people only).</summary>
    [Display(Name = "Icon")]
    public string? Icon { get; set; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Choose a colour.")]
    [Display(Name = "Map marker colour")]
    public string MarkerColor { get; set; } = "#7A1531";

    [Display(Name = "Show how many of these places there are on the home page")]
    public bool ShowInStats { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Order in lists")]
    public int SortOrder { get; set; }

    [Display(Name = "Show on the website")]
    public bool IsActive { get; set; } = true;

    public bool HasIcon => Kind != CategoryKind.Gallery;
    public bool IsPlaceCategory => Kind == CategoryKind.Places;
    public bool IsNew => Id is null;
}

public sealed record CategoryListItem(
    int Id, string Name, string? NameGu, string Slug, string? Icon, string? Color,
    bool ShowInStats, int SortOrder, bool IsActive, int UsageCount);

public sealed class CategoryListViewModel
{
    public required CategoryKind Kind { get; init; }
    public required IReadOnlyList<CategoryListItem> Items { get; init; }
}

/// <summary>UsageCount includes deleted items still in the bin: they keep their category until removed for good.</summary>
public sealed record CategoryDeleteViewModel(CategoryKind Kind, int Id, string Name, int UsageCount);
