using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.ViewModels.History;

namespace Khaparwada.Web.Services.History;

/// <summary>
/// The column selections shared by Home, About and the History page, so all three show the same text in the
/// same language. "Gujarati, else English" is decided in SQL, so only one language leaves the database.
/// </summary>
public static class HistoryQueries
{
    public static IQueryable<TimelineItemViewModel> SelectTimelineItems(this IQueryable<HistoryEntry> entries, bool gujarati) =>
        entries.Select(entry => new TimelineItemViewModel
        {
            Id = entry.Id,
            YearLabel = gujarati ? entry.YearLabelGu ?? entry.YearLabel : entry.YearLabel,
            Title = gujarati ? entry.TitleGu ?? entry.Title : entry.Title,
            Description = gujarati ? entry.DescriptionGu ?? entry.Description : entry.Description,
            ImagePath = entry.ImagePath,
            IsHighlighted = entry.IsHighlighted
        });

    public static IQueryable<ThenNowViewModel> SelectThenNow(this IQueryable<ThenNowComparison> comparisons, bool gujarati) =>
        comparisons.Select(comparison => new ThenNowViewModel
        {
            Title = gujarati ? comparison.TitleGu ?? comparison.Title : comparison.Title,
            Caption = gujarati ? comparison.CaptionGu ?? comparison.Caption : comparison.Caption,
            ThenLabel = gujarati ? comparison.ThenLabelGu ?? comparison.ThenLabel : comparison.ThenLabel,
            NowLabel = gujarati ? comparison.NowLabelGu ?? comparison.NowLabel : comparison.NowLabel,
            ThenImagePath = comparison.ThenImagePath,
            NowImagePath = comparison.NowImagePath
        });

    /// <summary>Timeline order: by year, and entries of the same year in the order they were added.</summary>
    public static IOrderedQueryable<HistoryEntry> InTimelineOrder(this IQueryable<HistoryEntry> entries) =>
        entries.OrderBy(entry => entry.SortYear).ThenBy(entry => entry.Id);

    public static IOrderedQueryable<ThenNowComparison> InDisplayOrder(this IQueryable<ThenNowComparison> comparisons) =>
        comparisons.OrderBy(comparison => comparison.SortOrder).ThenBy(comparison => comparison.Id);
}
