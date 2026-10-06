using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>A link on the Contact page; links with <see cref="ShowInFooter"/> also appear under "Important Links".</summary>
public class UsefulLink : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? TitleGu { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool ShowInFooter { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
