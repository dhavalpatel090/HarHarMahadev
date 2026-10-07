using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface IMessageAdminService
{
    Task<MessageListViewModel> ListAsync(MessageListFilter filter, int? page, CancellationToken cancellationToken = default);

    /// <summary>One message; opening it marks it read, as in an email program.</summary>
    Task<MessageDetailsViewModel?> OpenAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> MarkUnreadAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Deletes the message for good: it holds a resident's personal details and is no use once handled.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>The Contact form's inbox (Admins only). Messages are never shown on the website.</summary>
public sealed class MessageAdminService(AppDbContext db, IVillageClock clock) : IMessageAdminService
{
    public const int PageSize = 25;
    private const string ListRouteName = "admin-messages";

    public async Task<MessageListViewModel> ListAsync(MessageListFilter filter, int? page, CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<MessageListFilter, int>
        {
            [MessageListFilter.Unread] = await db.ContactMessages.CountAsync(m => !m.IsRead, cancellationToken),
            [MessageListFilter.All] = await db.ContactMessages.CountAsync(cancellationToken)
        };

        var query = db.ContactMessages.AsNoTracking();
        if (filter == MessageListFilter.Unread)
        {
            query = query.Where(m => !m.IsRead);
        }

        var total = counts[filter];
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new { m.Id, m.Name, m.Subject, m.Message, m.CreatedAt, m.IsRead })
            .ToListAsync(cancellationToken);

        return new MessageListViewModel
        {
            Items = rows.Select(row => new MessageListItem(
                row.Id, row.Name, row.Subject, TextHelpers.Excerpt(row.Message, 120), clock.ToVillageTime(row.CreatedAt), row.IsRead)).ToList(),
            Counts = counts,
            Filter = filter,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = ListRouteName,
                RouteValues = new Dictionary<string, string?> { ["show"] = filter == MessageListFilter.All ? "all" : null }
            }
        };
    }

    public async Task<MessageDetailsViewModel?> OpenAsync(int id, CancellationToken cancellationToken = default)
    {
        var message = await db.ContactMessages.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return null;
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            await db.SaveChangesAsync(cancellationToken);
        }

        return new MessageDetailsViewModel
        {
            Id = message.Id,
            Name = message.Name,
            Email = message.Email,
            Phone = message.Phone,
            Subject = message.Subject,
            Message = message.Message,
            ReceivedAt = clock.ToVillageTime(message.CreatedAt)
        };
    }

    public async Task<bool> MarkUnreadAsync(int id, CancellationToken cancellationToken = default) =>
        await db.ContactMessages.Where(m => m.Id == id).ExecuteUpdateAsync(set => set.SetProperty(m => m.IsRead, false), cancellationToken) > 0;

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        // ExecuteDelete bypasses the soft-delete interceptor: the row is gone, personal details included.
        await db.ContactMessages.IgnoreQueryFilters().Where(m => m.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
