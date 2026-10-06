using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Site;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface IUsefulLinkAdminService
{
    Task<IReadOnlyList<UsefulLinkListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<UsefulLinkInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the link when input.Id is null, otherwise updates it.</summary>
    Task<OperationResult> SaveAsync(UsefulLinkInputModel input, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Links for the Contact page and the footer. Every change refreshes the cached footer.</summary>
public sealed class UsefulLinkAdminService(AppDbContext db, ISiteInfoService siteInfo) : IUsefulLinkAdminService
{
    public async Task<IReadOnlyList<UsefulLinkListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.UsefulLinks.AsNoTracking()
            .OrderBy(link => link.SortOrder).ThenBy(link => link.Title)
            .Select(link => new UsefulLinkListItem(link.Id, link.Title, link.TitleGu, link.Url, link.ShowInFooter, link.SortOrder, link.IsActive))
            .ToListAsync(cancellationToken);

    public Task<UsefulLinkInputModel?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        db.UsefulLinks.AsNoTracking()
            .Where(link => link.Id == id)
            .Select(link => new UsefulLinkInputModel
            {
                Id = link.Id,
                Title = link.Title,
                TitleGu = link.TitleGu,
                Url = link.Url,
                ShowInFooter = link.ShowInFooter,
                SortOrder = link.SortOrder,
                IsActive = link.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OperationResult> SaveAsync(UsefulLinkInputModel input, CancellationToken cancellationToken = default)
    {
        UsefulLink? link;
        if (input.Id is { } id)
        {
            link = await db.UsefulLinks.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (link is null)
            {
                return OperationResult.Failure(string.Empty, "This link no longer exists.");
            }
        }
        else
        {
            link = new UsefulLink();
            db.UsefulLinks.Add(link);
        }

        link.Title = input.Title.Trim();
        link.TitleGu = string.IsNullOrWhiteSpace(input.TitleGu) ? null : input.TitleGu.Trim();
        link.Url = input.Url.Trim();
        link.ShowInFooter = input.ShowInFooter;
        link.SortOrder = input.SortOrder;
        link.IsActive = input.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        siteInfo.Invalidate();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await db.UsefulLinks.Where(link => link.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return OperationResult.Failure(string.Empty, "This link no longer exists.");
        }

        siteInfo.Invalidate();
        return OperationResult.Success();
    }
}
