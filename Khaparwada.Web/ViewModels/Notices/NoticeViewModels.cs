using Khaparwada.Web.Models.Enums;

namespace Khaparwada.Web.ViewModels.Notices;

public sealed class NoticeItemViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required NoticeCategory Category { get; init; }
    public required NoticePriority Priority { get; init; }

    /// <summary>Publish time, already converted to the village's time zone.</summary>
    public required DateTimeOffset PublishedAt { get; init; }

    public string Icon => NoticeDisplay.IconFor(Category);
    public string CategoryLabel => NoticeDisplay.LabelFor(Category);
}

public sealed record NoticeAlertViewModel(string Slug, string Title);

/// <summary>Icon and label (localization key) for each notice category.</summary>
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
}
