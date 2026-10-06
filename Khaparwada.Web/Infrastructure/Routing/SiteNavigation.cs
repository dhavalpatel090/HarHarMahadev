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

    /// <summary>ViewData key a page sets to highlight its menu item (e.g. a place's page highlights "Places").</summary>
    public const string SectionKey = "Section";
}
