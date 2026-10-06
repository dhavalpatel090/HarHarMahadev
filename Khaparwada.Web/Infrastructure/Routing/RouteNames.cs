namespace Khaparwada.Web.Infrastructure.Routing;

/// <summary>
/// Names of the public routes. Views link with <c>asp-route="@RouteNames.Places"</c>, so links keep working
/// when a "coming soon" placeholder is replaced by the real page in a later phase: the name moves with it.
/// </summary>
public static class RouteNames
{
    public const string Home = "home";
    public const string About = "about";
    public const string History = "history";
    public const string Places = "places";
    public const string PlaceDetails = "place-details";
    public const string Map = "map";
    public const string People = "people";
    public const string PersonDetails = "person-details";
    public const string Events = "events";
    public const string EventDetails = "event-details";
    public const string Gallery = "gallery";
    public const string AlbumDetails = "album-details";
    public const string Notices = "notices";
    public const string NoticeDetails = "notice-details";
    public const string Memories = "memories";
    public const string MemoryDetails = "memory-details";
    public const string ShareMemory = "share-memory";
    public const string Contact = "contact";
    public const string Privacy = "privacy";
    public const string Terms = "terms";
}
