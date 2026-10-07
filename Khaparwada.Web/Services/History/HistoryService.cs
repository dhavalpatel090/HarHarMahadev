using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Site;
using Khaparwada.Web.ViewModels.History;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.History;

public interface IHistoryService
{
    Task<HistoryPageViewModel> GetPageAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The History page: the whole published timeline and every published "then and now" pair. A village
/// timeline holds tens of entries, not thousands, so it is shown on one page without paging.
/// </summary>
public sealed class HistoryService(AppDbContext db, ISiteInfoService siteInfo) : IHistoryService
{
    public async Task<HistoryPageViewModel> GetPageAsync(CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;

        return new HistoryPageViewModel
        {
            Site = await siteInfo.GetAsync(cancellationToken),
            Entries = await db.HistoryEntries.AsNoTracking()
                .Visible()
                .InTimelineOrder()
                .SelectTimelineItems(gujarati)
                .ToListAsync(cancellationToken),
            ThenNow = await db.ThenNowComparisons.AsNoTracking()
                .Visible()
                .InDisplayOrder()
                .SelectThenNow(gujarati)
                .ToListAsync(cancellationToken)
        };
    }
}
