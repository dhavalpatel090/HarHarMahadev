using Khaparwada.Web.Infrastructure.Media;

namespace Khaparwada.Web.ViewModels.History;

/// <summary>A "then and now" pair of photos, shown with a before/after slider on Home and the History page.</summary>
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
