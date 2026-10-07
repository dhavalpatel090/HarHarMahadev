using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.History;

public sealed class HistoryPageViewModel
{
    public required SiteInfo Site { get; init; }

    /// <summary>Every published entry, oldest first.</summary>
    public required IReadOnlyList<TimelineItemViewModel> Entries { get; init; }

    public required IReadOnlyList<ThenNowViewModel> ThenNow { get; init; }
}
