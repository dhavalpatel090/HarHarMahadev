using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Khaparwada.Web.Services.Site;

public interface ISiteInfoService
{
    /// <summary>Site and village details in the current language. Read from memory; the database is hit at most every 10 minutes.</summary>
    Task<SiteInfo> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Call after an admin edits settings, village info or footer links, so the next page shows the change.</summary>
    void Invalidate();
}

/// <summary>
/// Every page needs the site settings (header, footer, meta tags). Reading them from the database on every
/// request would add three queries per page view, so one language-neutral snapshot is kept in memory and
/// localized per request.
/// </summary>
public sealed class SiteInfoService(AppDbContext db, IMemoryCache cache) : ISiteInfoService
{
    private const string CacheKey = "site-info";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<SiteInfo> GetAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await LoadAsync(cancellationToken);
        });

        return Localize(snapshot!);
    }

    public void Invalidate() => cache.Remove(CacheKey);

    private async Task<Snapshot> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        var village = await db.VillageInfo.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (settings is null || village is null)
        {
            throw new InvalidOperationException(
                "The SiteSettings or VillageInfo row is missing. Restart the app: the startup seeder re-creates it.");
        }

        var footerLinks = await db.UsefulLinks.AsNoTracking()
            .Where(link => link.IsActive && link.ShowInFooter)
            .OrderBy(link => link.SortOrder)
            .ToListAsync(cancellationToken);

        return new Snapshot(settings, village, footerLinks);
    }

    private static SiteInfo Localize(Snapshot snapshot)
    {
        var (settings, village, links) = snapshot;

        var social = new List<SocialLink>();
        AddSocial(social, "Facebook", settings.FacebookUrl, "facebook");
        AddSocial(social, "Instagram", settings.InstagramUrl, "instagram");
        AddSocial(social, "YouTube", settings.YouTubeUrl, "youtube");
        AddSocial(social, "WhatsApp", settings.WhatsAppUrl, "whatsapp");

        return new SiteInfo
        {
            SiteName = AppLanguage.Pick(settings.SiteName, settings.SiteNameGu),
            Tagline = AppLanguage.Pick(settings.Tagline, settings.TaglineGu),
            MetaDescription = AppLanguage.PickOptional(settings.DefaultMetaDescription, settings.DefaultMetaDescriptionGu),
            VillageName = AppLanguage.Pick(village.Name, village.NameGu),
            VillageIntro = AppLanguage.Pick(village.ShortIntro, village.ShortIntroGu),
            Population = village.Population,
            Households = village.Households,
            AreaSqKm = village.AreaSqKm,
            StatsSource = AppLanguage.PickOptional(village.StatsSource, village.StatsSourceGu),
            Taluka = AppLanguage.PickOptional(village.Taluka, village.TalukaGu),
            District = AppLanguage.PickOptional(village.District, village.DistrictGu),
            State = AppLanguage.PickOptional(village.State, village.StateGu),
            PinCode = village.PinCode,
            Latitude = village.Latitude,
            Longitude = village.Longitude,
            MapZoom = village.MapZoom,
            OfficeEmail = settings.OfficeEmail,
            OfficePhone = settings.ShowOfficePhone ? settings.OfficePhone : null,
            OfficeAddress = AppLanguage.PickOptional(settings.OfficeAddress, settings.OfficeAddressGu),
            HeroImageUrl = MediaUrls.Image(settings.HeroImagePath, ImageSize.Large),
            HeroVideoUrl = string.IsNullOrWhiteSpace(settings.HeroVideoPath) ? null : $"{MediaUrls.RequestPath}/{settings.HeroVideoPath}",
            SocialLinks = social,
            FooterLinks = links.Select(link => new ExternalLink(AppLanguage.Pick(link.Title, link.TitleGu), link.Url)).ToList()
        };
    }

    private static void AddSocial(List<SocialLink> links, string name, string? url, string icon)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            links.Add(new SocialLink(name, url, icon));
        }
    }

    private sealed record Snapshot(SiteSetting Settings, VillageInfo Village, IReadOnlyList<UsefulLink> FooterLinks);
}
