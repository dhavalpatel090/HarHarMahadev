using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>An album's details. Its photos are managed on the album's Photos page.</summary>
public sealed class AlbumInputModel
{
    /// <summary>From the URL, never from the form; null while creating.</summary>
    [BindNever]
    public int? Id { get; set; }

    [Required(ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Enter the album's name.")]
    [StringLength(FieldLengths.Title)]
    [Display(Name = "Name of the album")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLengths.Title)]
    [Display(Name = "Name of the album")]
    public string? TitleGu { get; set; }

    [StringLength(FieldLengths.Slug)]
    [RegularExpression(SlugGenerator.Pattern, ErrorMessage = "Use lowercase English letters, numbers and single hyphens, e.g. navratri-2026.")]
    [Display(Name = "Web address")]
    public string? Slug { get; set; }

    [StringLength(FieldLengths.LongText)]
    [Display(Name = "About these photos")]
    public string? Description { get; set; }

    [StringLength(FieldLengths.LongText)]
    [Display(Name = "About these photos")]
    public string? DescriptionGu { get; set; }

    [Display(Name = "Taken on")]
    public DateOnly? TakenOn { get; set; }

    [Display(Name = "From the event")]
    public int? EventId { get; set; }

    [Display(Name = "Shown on the website (once it has an approved photo)")]
    public bool IsPublished { get; set; } = true;

    [BindNever]
    public IReadOnlyList<PlaceCategoryOption> Categories { get; set; } = [];

    [BindNever]
    public IReadOnlyList<AlbumEventOption> Events { get; set; } = [];

    public bool IsNew => Id is null;
}

/// <summary>An event in the album form's list, newest first ("Navratri Garba Night, 17 Oct 2026").</summary>
public sealed record AlbumEventOption(int Id, string Label);

public sealed record AlbumListItem(
    int Id, string Title, string? TitleGu, string CategoryName, bool CategoryIsActive, DateOnly? TakenOn,
    string? CoverPath, int Shown, int Waiting, bool IsPublished, bool IsPlaceholder);

public sealed class AlbumAdminListViewModel
{
    public required IReadOnlyList<AlbumListItem> Items { get; init; }
    public required IReadOnlyList<PlaceCategoryOption> Categories { get; init; }
    public int? CategoryId { get; init; }
    public string? Search { get; init; }
    public int WaitingTotal { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>An album's Photos page: upload, captions, order, removal.</summary>
public sealed class AlbumPhotosForm
{
    [BindNever]
    public int AlbumId { get; set; }

    [BindNever]
    public string AlbumTitle { get; set; } = string.Empty;

    [BindNever]
    public string AlbumSlug { get; set; } = string.Empty;

    [BindNever]
    public bool AlbumIsPublished { get; set; }

    public List<AlbumPhotoRow> Photos { get; set; } = [];
}

public sealed class AlbumPhotoRow
{
    public int Id { get; set; }

    /// <summary>Shown only; never taken from the form.</summary>
    [BindNever]
    public string ImagePath { get; set; } = string.Empty;

    [BindNever]
    public ModerationStatus Status { get; set; }

    [BindNever]
    public string? UploadedBy { get; set; }

    [StringLength(FieldLengths.Caption)]
    [Display(Name = "Caption")]
    public string? Caption { get; set; }

    [StringLength(FieldLengths.Caption)]
    [Display(Name = "Caption (Gujarati)")]
    public string? CaptionGu { get; set; }

    [Range(0, 999999)]
    [Display(Name = "Order")]
    public int SortOrder { get; set; }

    [Display(Name = "Remove for good")]
    public bool Remove { get; set; }
}

/// <summary>Photos waiting for an Admin, from every album, oldest first (first come, first reviewed).</summary>
public sealed class PhotoReviewViewModel
{
    public required IReadOnlyList<PhotoReviewItem> Items { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

public sealed record PhotoReviewItem(
    int Id, string ImagePath, int Width, int Height, string? Caption, int AlbumId, string AlbumTitle,
    string? UploadedBy, DateTimeOffset UploadedAt);

/// <summary>The review form: the ticked photos and what to do with them.</summary>
public sealed class PhotoReviewForm
{
    public List<int> Selected { get; set; } = [];

    /// <summary>"approve" or "delete".</summary>
    public string? Decision { get; set; }
}
