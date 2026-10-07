using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.Notices;

public sealed class NoticeItemViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required NoticeCategory Category { get; init; }
    public required NoticePriority Priority { get; init; }

    /// <summary>Publish time, already converted to the village's time zone.</summary>
    public required DateTimeOffset PublishedAt { get; init; }

    /// <summary>The last day the notice is shown, if it has an end.</summary>
    public DateOnly? ValidUntil { get; init; }

    /// <summary>The start of the text, on the notice board only (Home shows titles).</summary>
    public string? Excerpt { get; init; }

    public string Icon => NoticeDisplay.IconFor(Category);
    public string CategoryLabel => NoticeDisplay.LabelFor(Category);
}

public sealed record NoticeAlertViewModel(string Slug, string Title);

/// <summary>A category chip on /notices; Count = notices on the board now.</summary>
public sealed record NoticeCategoryViewModel(NoticeCategory Category, int Count)
{
    public string Slug => NoticeDisplay.SlugFor(Category);
    public string Label => NoticeDisplay.LabelFor(Category);
    public string Icon => NoticeDisplay.IconFor(Category);
}

/// <summary>/notices: the board, optionally one category: urgent first, then important, then newest.</summary>
public sealed class NoticeListViewModel
{
    public required IReadOnlyList<NoticeCategoryViewModel> Categories { get; init; }
    public NoticeCategoryViewModel? Category { get; init; }
    public required IReadOnlyList<NoticeItemViewModel> Notices { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>/notices/{slug} while the notice is on the board. Times are in village time.</summary>
public sealed class NoticeDetailsViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required NoticeCategory Category { get; init; }
    public required NoticePriority Priority { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public DateOnly? ValidUntil { get; init; }
    public string? ImagePath { get; init; }

    /// <summary>The attachment's file name and size, when the notice has one (opened at /notices/{slug}/attachment).</summary>
    public string? AttachmentName { get; init; }
    public long? AttachmentBytes { get; init; }

    public required IReadOnlyList<NoticeItemViewModel> MoreNotices { get; init; }

    public string Icon => NoticeDisplay.IconFor(Category);
    public string CategoryLabel => NoticeDisplay.LabelFor(Category);
    public string CategorySlug => NoticeDisplay.SlugFor(Category);
    public string? ImageUrl => MediaUrls.Image(ImagePath, ImageSize.Large);
}

/// <summary>What /notices/{slug} found: a notice on the board, one that has ended, or nothing.</summary>
public enum NoticeLookupState
{
    Missing,
    Active,
    Ended
}

/// <summary>For an ended notice only the title and last day are kept: its text may be out of date.</summary>
public sealed record NoticeLookup(NoticeLookupState State, NoticeDetailsViewModel? Notice = null, string? EndedTitle = null, DateOnly? EndedOn = null);

/// <summary>A notice's attachment to send: the private file and the name to offer.</summary>
public sealed record NoticeAttachmentFile(string FullPath, string FileName);

/// <summary>Icon, label (localization key) and address part (/notices?category=water) for each notice category.</summary>
public static class NoticeDisplay
{
    public static string IconFor(NoticeCategory category) => category switch
    {
        NoticeCategory.GovernmentScheme => "file-signature",
        NoticeCategory.Water => "droplet",
        NoticeCategory.Electricity => "bolt",
        NoticeCategory.Meeting => "people-group",
        NoticeCategory.Emergency => "triangle-exclamation",
        _ => "bullhorn"
    };

    public static string LabelFor(NoticeCategory category) => category switch
    {
        NoticeCategory.GovernmentScheme => "Government scheme",
        NoticeCategory.Water => "Water",
        NoticeCategory.Electricity => "Electricity",
        NoticeCategory.Meeting => "Meeting",
        NoticeCategory.Emergency => "Emergency",
        _ => "General"
    };

    public static string SlugFor(NoticeCategory category) => category switch
    {
        NoticeCategory.GovernmentScheme => "government-scheme",
        NoticeCategory.Water => "water",
        NoticeCategory.Electricity => "electricity",
        NoticeCategory.Meeting => "meeting",
        NoticeCategory.Emergency => "emergency",
        _ => "general"
    };

    /// <summary>Only the address parts above; never numbers or enum names typed into the address.</summary>
    public static NoticeCategory? FromSlug(string? slug) =>
        Enum.GetValues<NoticeCategory>().Cast<NoticeCategory?>()
            .FirstOrDefault(category => string.Equals(SlugFor(category!.Value), slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The last day a notice is shown. Expiry is a moment ("ends at midnight after 20 Oct"); people think in days,
    /// so the moment just before it is shown as the day.
    /// </summary>
    public static DateOnly? LastDay(DateTimeOffset? expiresAtVillageTime) =>
        expiresAtVillageTime is { } expires ? DateOnly.FromDateTime(expires.AddTicks(-1).DateTime) : null;
}
