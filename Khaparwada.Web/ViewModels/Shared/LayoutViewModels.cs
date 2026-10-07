namespace Khaparwada.Web.ViewModels.Shared;

/// <summary>Everything the &lt;head&gt; needs for search engines and link previews (WhatsApp, Facebook).</summary>
public sealed class PageMetaViewModel
{
    public required string Title { get; init; }
    public required string ShareTitle { get; init; }
    public string? Description { get; init; }
    public required string CanonicalUrl { get; init; }
    public required string EnglishUrl { get; init; }
    public required string GujaratiUrl { get; init; }
    public required string ImageUrl { get; init; }
    public required string SiteName { get; init; }
    public required string Locale { get; init; }
    public required string AlternateLocale { get; init; }
}

/// <summary>The banner at the top of inner pages: breadcrumb, title and a short introduction.</summary>
public sealed class PageHeaderViewModel
{
    public required string Title { get; init; }
    public string? Lead { get; init; }
    public string? Icon { get; init; }

    /// <summary>Trail after "Home". The last item is the current page and is not a link.</summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; init; } = [];
}

public sealed record BreadcrumbItem(string Text, string? RouteName = null);

/// <summary>Eyebrow, title, lead and an optional "see all" link above a section (to a route, optionally an anchor on it).</summary>
public sealed record SectionHeadingViewModel(
    string Eyebrow,
    string Title,
    string TitleId,
    string? Lead = null,
    string? LinkText = null,
    string? LinkRoute = null,
    string? LinkFragment = null);

/// <summary>Placeholder for a section that is built in a later phase. Title and Lead are localization keys.</summary>
public sealed record ComingSoonViewModel(string Title, string Lead, string Icon);
