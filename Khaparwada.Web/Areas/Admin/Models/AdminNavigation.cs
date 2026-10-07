namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>
/// The admin sidebar. Items without a controller are screens that later phases add; they are shown
/// greyed out so the admin can see what is coming.
/// </summary>
public static class AdminNavigation
{
    public sealed record Item(string Label, string Icon, string? Controller, bool AdminOnly = false, string? ComingIn = null);

    public sealed record Group(string Label, IReadOnlyList<Item> Items);

    public static IReadOnlyList<Group> Groups { get; } =
    [
        new("Overview",
        [
            new Item("Dashboard", "house", "Dashboard")
        ]),
        new("Content",
        [
            new Item("About page", "book-open", "AboutSections"),
            new Item("History timeline", "clock-rotate-left", "Timeline"),
            new Item("Then & now", "arrows-left-right", "ThenNow"),
            new Item("Places", "location-dot", "Places"),
            new Item("People", "people-group", "People"),
            new Item("Events", "calendar-days", "Events"),
            new Item("Gallery", "images", "Gallery"),
            new Item("Notices", "bullhorn", null, ComingIn: "Phase 9"),
            new Item("Memories", "camera", null, ComingIn: "Phase 10"),
            new Item("Messages", "envelope", null, ComingIn: "Phase 9")
        ]),
        new("Settings",
        [
            new Item("Village info", "landmark", "VillageInfo", AdminOnly: true),
            new Item("Site settings", "globe", "SiteSettings", AdminOnly: true),
            new Item("Categories", "compass", "Categories", AdminOnly: true),
            new Item("Useful links", "arrow-up-right-from-square", "UsefulLinks", AdminOnly: true),
            new Item("Users", "users", null, AdminOnly: true, ComingIn: "Phase 11")
        ])
    ];
}
