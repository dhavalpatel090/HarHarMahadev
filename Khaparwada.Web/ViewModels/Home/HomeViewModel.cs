using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.ViewModels.Events;
using Khaparwada.Web.ViewModels.Gallery;
using Khaparwada.Web.ViewModels.History;
using Khaparwada.Web.ViewModels.Map;
using Khaparwada.Web.ViewModels.Memories;
using Khaparwada.Web.ViewModels.Notices;
using Khaparwada.Web.ViewModels.People;
using Khaparwada.Web.ViewModels.Places;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.Home;

public sealed class HomeViewModel
{
    public required SiteInfo Site { get; init; }
    public required HeroViewModel Hero { get; init; }
    public required IReadOnlyList<StatViewModel> Stats { get; init; }
    public required IReadOnlyList<VillageFactViewModel> Facts { get; init; }
    public required IReadOnlyList<CategoryTileViewModel> PlaceCategories { get; init; }
    public required IReadOnlyList<PlaceCardViewModel> FeaturedPlaces { get; init; }
    public required IReadOnlyList<TimelineItemViewModel> HistoryHighlights { get; init; }
    public required IReadOnlyList<ThenNowViewModel> ThenNow { get; init; }
    public required IReadOnlyList<EventCardViewModel> UpcomingEvents { get; init; }
    public required IReadOnlyList<NoticeItemViewModel> LatestNotices { get; init; }
    public required IReadOnlyList<AlbumCardViewModel> Albums { get; init; }
    public required IReadOnlyList<PersonCardViewModel> FeaturedPeople { get; init; }
    public required IReadOnlyList<MemoryCardViewModel> Memories { get; init; }
    public MapViewModel? Map { get; init; }
}

public sealed class HeroViewModel
{
    public required string Title { get; init; }
    public required string Tagline { get; init; }
    public required string VillageName { get; init; }
    public required string ImageUrl { get; init; }

    /// <summary>True while the default illustration is shown because no photo has been uploaded.</summary>
    public bool IsIllustration { get; init; }
    public string? VideoUrl { get; init; }
}

public sealed record StatViewModel(string Icon, string Value, string Label);

public sealed record VillageFactViewModel(string Label, string Value);

public sealed class ThenNowViewModel
{
    public required string Title { get; init; }
    public string? Caption { get; init; }
    public required string ThenLabel { get; init; }
    public required string NowLabel { get; init; }
    public string? ThenImagePath { get; init; }
    public string? NowImagePath { get; init; }

    public string ThenImageUrl => MediaUrls.Image(ThenImagePath, ImageSize.Large) ?? MediaUrls.VillageIllustration;
    public string NowImageUrl => MediaUrls.Image(NowImagePath, ImageSize.Large) ?? MediaUrls.VillageIllustration;

    /// <summary>Until a real "then" photo exists, the illustration is shown in sepia to suggest an old photograph.</summary>
    public bool ThenIsPlaceholder => string.IsNullOrWhiteSpace(ThenImagePath);
}
