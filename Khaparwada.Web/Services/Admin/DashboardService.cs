using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface IDashboardService
{
    Task<DashboardViewModel> GetAsync(string displayName, CancellationToken cancellationToken = default);
}

/// <summary>Counts for the admin dashboard, what is waiting for review, and which setup steps are still open.</summary>
public sealed class DashboardService(AppDbContext db, IVillageClock clock) : IDashboardService
{
    private const string SamplePrefix = "[Sample]";

    public async Task<DashboardViewModel> GetAsync(string displayName, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var ct = cancellationToken;

        var stats = new List<DashboardStat>
        {
            new("Places", await db.Places.CountAsync(ct), "location-dot",
                $"{await db.Places.Visible().CountAsync(ct)} published"),
            new("Events", await db.Events.CountAsync(ct), "calendar-days",
                $"{await db.Events.Upcoming(now).CountAsync(ct)} upcoming"),
            new("Gallery photos", await db.GalleryImages.CountAsync(ct), "images",
                $"{await db.GalleryAlbums.CountAsync(ct)} albums"),
            new("Notices", await db.Notices.CountAsync(ct), "bullhorn",
                $"{await db.Notices.Active(now).CountAsync(ct)} on the board now"),
            new("People", await db.People.CountAsync(ct), "people-group",
                $"{await db.People.Visible().CountAsync(ct)} published")
        };

        var pending = new List<PendingItem>
        {
            new("Memories to review", await db.MemorySubmissions.CountAsync(m => m.Status == ModerationStatus.Pending || m.Status == ModerationStatus.UnderReview, ct), "camera"),
            new("Profiles to approve", await db.People.CountAsync(p => p.Status == ModerationStatus.Pending || p.Status == ModerationStatus.UnderReview, ct), "people-group"),
            new("Photos to approve", await db.GalleryImages.CountAsync(i => i.Status == ModerationStatus.Pending || i.Status == ModerationStatus.UnderReview, ct), "images"),
            new("Unread messages", await db.ContactMessages.CountAsync(m => !m.IsRead, ct), "envelope")
        };
        stats.Add(new DashboardStat("Waiting for review", pending.Sum(item => item.Count), "inbox", "memories, profiles, photos, messages"));

        return new DashboardViewModel
        {
            DisplayName = displayName,
            Stats = stats,
            Pending = pending,
            Setup = await GetSetupStepsAsync(ct)
        };
    }

    /// <summary>The placeholders from the first start ("[Add taluka]" etc.) still waiting for real information.</summary>
    private async Task<List<SetupStep>> GetSetupStepsAsync(CancellationToken ct)
    {
        var village = await db.VillageInfo.AsNoTracking().SingleAsync(ct);
        var settings = await db.SiteSettings.AsNoTracking().SingleAsync(ct);

        var sampleItems =
            await db.Places.CountAsync(p => p.Name.StartsWith(SamplePrefix), ct) +
            await db.Events.CountAsync(e => e.Title.StartsWith(SamplePrefix), ct) +
            await db.Notices.CountAsync(n => n.Title.StartsWith(SamplePrefix), ct) +
            await db.People.CountAsync(p => p.FullName.StartsWith(SamplePrefix), ct) +
            await db.GalleryAlbums.CountAsync(a => a.Title.StartsWith(SamplePrefix), ct) +
            await db.HistoryEntries.CountAsync(h => h.Title.StartsWith(SamplePrefix), ct) +
            await db.ThenNowComparisons.CountAsync(t => t.Title.StartsWith(SamplePrefix), ct) +
            await db.MemorySubmissions.CountAsync(m => m.Title.StartsWith(SamplePrefix), ct);

        return
        [
            new("Write the village introduction", "Shown on the home page under “About”.", IsFilled(village.ShortIntro), "VillageInfo"),
            new("Add taluka, district and state", "Shown in the footer and in the village facts.",
                IsFilled(village.Taluka) && IsFilled(village.District) && IsFilled(village.State), "VillageInfo"),
            new("Mark the village centre on the map", "Click the map on the Village info page.", village.Latitude is not null, "VillageInfo"),
            new("Add the village name in Gujarati", "Used on the Gujarati pages.", IsFilled(village.NameGu), "VillageInfo"),
            new("Add population and families", "Shown in the statistics on the home page.",
                village.Population is not null && village.Households is not null, "VillageInfo"),
            new("Upload a photo of the village", "Replaces the illustration at the top of the home page.", settings.HeroImagePath is not null, "SiteSettings"),
            new("Add the office contact details", "Shown in the contact strip on the home page.",
                IsFilled(settings.OfficeEmail) || IsFilled(settings.OfficePhone), "SiteSettings"),
            new("Replace the sample content", sampleItems == 0
                    ? "All sample items are gone."
                    : $"{sampleItems} items marked [Sample]. Their admin screens arrive from Phase 4.",
                sampleItems == 0, null)
        ];
    }

    private static bool IsFilled(string? value) => !string.IsNullOrWhiteSpace(value) && !value.TrimStart().StartsWith('[');
}
