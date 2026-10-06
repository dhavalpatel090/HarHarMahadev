using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// One block on the About page (introduction, culture, traditions, festivals, geography, …).
/// Body is plain text; a blank line starts a new paragraph. No HTML is stored, so there is no
/// stored HTML that could carry a script into the page.
/// </summary>
public class AboutSection : BaseEntity, ISoftDeletable
{
    public string Slug { get; set; } = string.Empty;    // anchor on the About page, e.g. /about#culture
    public string Title { get; set; } = string.Empty;
    public string? TitleGu { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? BodyGu { get; set; }
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
