namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class DashboardViewModel
{
    public required string DisplayName { get; init; }
    public required IReadOnlyList<DashboardStat> Stats { get; init; }
    public required IReadOnlyList<PendingItem> Pending { get; init; }
    public required IReadOnlyList<SetupStep> Setup { get; init; }

    public int PendingTotal => Pending.Sum(item => item.Count);
    public int SetupDone => Setup.Count(step => step.Done);
}

public sealed record DashboardStat(string Label, int Value, string Icon, string? Detail = null);

/// <summary>
/// Something waiting for a person to act on. Controller is null until that admin screen exists; Status is the
/// list tab it opens (e.g. "review"), Action the screen (default: the list).
/// </summary>
public sealed record PendingItem(string Label, int Count, string Icon, string? Controller = null, string? Status = null, string Action = "Index");

/// <summary>
/// One step of "finish setting up": replacing a placeholder with real village information. AdminOnly steps
/// link to screens an Editor may not open, so Editors see no "Open" button for them.
/// </summary>
public sealed record SetupStep(string Label, string Hint, bool Done, string? Controller, bool AdminOnly = false);
