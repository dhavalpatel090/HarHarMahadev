using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Infrastructure.Validation;
using Khaparwada.Web.Models.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class UsefulLinkInputModel
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

    [Required(ErrorMessage = "Enter the web address.")]
    [HttpUrl, StringLength(FieldLengths.Url)]
    [Display(Name = "Web address")]
    public string Url { get; set; } = string.Empty;

    [Display(Name = "Also show in the footer under “Important links”")]
    public bool ShowInFooter { get; set; }

    [Range(0, 9999)]
    [Display(Name = "Order in lists")]
    public int SortOrder { get; set; }

    [Display(Name = "Show on the website")]
    public bool IsActive { get; set; } = true;

    public bool IsNew => Id is null;
}

public sealed record UsefulLinkListItem(int Id, string Title, string? TitleGu, string Url, bool ShowInFooter, int SortOrder, bool IsActive);
