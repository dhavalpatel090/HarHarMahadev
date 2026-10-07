using Khaparwada.Web.ViewModels.History;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.About;

public sealed class AboutPageViewModel
{
    public required SiteInfo Site { get; init; }

    /// <summary>Taluka, district, state, PIN code, population, families, area: whatever is filled in.</summary>
    public required IReadOnlyList<VillageFactViewModel> Facts { get; init; }

    public required IReadOnlyList<AboutSectionViewModel> Sections { get; init; }

    /// <summary>Highlighted timeline entries ("Important milestones").</summary>
    public required IReadOnlyList<TimelineItemViewModel> Milestones { get; init; }

    /// <summary>
    /// Milestones are shown right after the History section, where a reader expects them; if there is no
    /// section with that address, after the last section.
    /// </summary>
    public bool MilestonesFollow(AboutSectionViewModel section) =>
        Sections.Any(s => s.Anchor == AboutPageAnchors.HistorySection)
            ? section.Anchor == AboutPageAnchors.HistorySection
            : ReferenceEquals(section, Sections[^1]);
}

public sealed class AboutSectionViewModel
{
    /// <summary>The section's web address, used as its id: /about#culture.</summary>
    public required string Anchor { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string? ImagePath { get; init; }
}

/// <summary>
/// A section's web address becomes an element id, and ids must be unique on a page: a clash would make
/// "#…" links and the skip link jump to the wrong place. So sections may not use the page's own ids, nor
/// anything starting with "kc-", the prefix of every other id in the layout and on this page.
/// </summary>
public static class AboutPageAnchors
{
    public const string HistorySection = "history";
    public const string Milestones = "milestones";
    public const string Facts = "village-facts";
    public const string Contents = "on-this-page";

    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal) { "main", Milestones, Facts, Contents };

    public static bool IsReserved(string slug) => Reserved.Contains(slug) || slug.StartsWith("kc-", StringComparison.Ordinal);
}
