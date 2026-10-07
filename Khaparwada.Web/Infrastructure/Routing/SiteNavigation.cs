namespace Khaparwada.Web.Infrastructure.Routing;

/// <summary>The main menu, shared by the navbar and the footer. Labels are localization keys (English text).</summary>
public static class SiteNavigation
{
    public sealed record NavItem(string RouteName, string Label, string Icon);

    public static IReadOnlyList<NavItem> Main { get; } =
    [
        new(RouteNames.Home, "Home", "house"),
        new(RouteNames.About, "About", "book-open"),
        new(RouteNames.History, "History", "clock-rotate-left"),
        new(RouteNames.Places, "Places", "location-dot"),
        new(RouteNames.Map, "Map", "map"),
        new(RouteNames.People, "People", "people-group"),
        new(RouteNames.Events, "Events", "calendar-days"),
        new(RouteNames.Gallery, "Gallery", "images"),
        new(RouteNames.Notices, "Notices", "bullhorn"),
        new(RouteNames.Contact, "Contact", "envelope")
    ];

    /// <summary>ViewData key a page can set to highlight a menu item other than its own route.</summary>
    public const string SectionKey = "Section";

    /// <summary>
    /// Pages inside a section have their own route names, so without this list no menu item would match them:
    /// a place's page highlights "Places", a profile "People".
    /// </summary>
    private static readonly Dictionary<string, string> ParentSections = new()
    {
        [RouteNames.PlaceDetails] = RouteNames.Places,
        [RouteNames.PersonDetails] = RouteNames.People,
        [RouteNames.EventDetails] = RouteNames.Events,
        [RouteNames.AlbumDetails] = RouteNames.Gallery,
        [RouteNames.NoticeDetails] = RouteNames.Notices
    };

    /// <summary>The menu item to highlight for a route: its section's, or its own.</summary>
    public static string? SectionFor(string? routeName) =>
        routeName is not null && ParentSections.TryGetValue(routeName, out var parent) ? parent : routeName;
}
