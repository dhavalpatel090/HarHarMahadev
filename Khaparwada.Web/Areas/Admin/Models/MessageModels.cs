using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>The tabs of the inbox (?show=all).</summary>
public enum MessageListFilter
{
    Unread,
    All
}

/// <summary>One message in the inbox list; time in village time.</summary>
public sealed record MessageListItem(int Id, string Name, string Subject, string Excerpt, DateTimeOffset ReceivedAt, bool IsRead);

public sealed class MessageListViewModel
{
    public required IReadOnlyList<MessageListItem> Items { get; init; }
    public required IReadOnlyDictionary<MessageListFilter, int> Counts { get; init; }
    public MessageListFilter Filter { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>One message, with ready-made reply links (the reply is written in the admin's own email or phone).</summary>
public sealed class MessageDetailsViewModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }

    public string? ReplyMailto => Email is null
        ? null
        : $"mailto:{Uri.EscapeDataString(Email)}?subject={Uri.EscapeDataString("Re: " + Subject)}";

    public string? PhoneHref => PhoneLinks.Tel(Phone);
}
