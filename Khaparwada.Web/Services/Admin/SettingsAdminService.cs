using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.Services.Site;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface ISettingsAdminService
{
    Task<VillageInfoInputModel> GetVillageInfoAsync(CancellationToken cancellationToken = default);
    Task SaveVillageInfoAsync(VillageInfoInputModel input, CancellationToken cancellationToken = default);
    Task<SiteSettingsInputModel> GetSiteSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves the settings, including a new or removed Home page photo and video.</summary>
    Task<OperationResult> SaveSiteSettingsAsync(SiteSettingsInputModel input, CancellationToken cancellationToken = default);
}

/// <summary>The two single-row tables: VillageInfo and SiteSettings. Every save refreshes the cached site details.</summary>
public sealed class SettingsAdminService(
    AppDbContext db,
    ISiteInfoService siteInfo,
    IImageUploadService images,
    IVideoUploadService videos) : ISettingsAdminService
{
    private const string MediaFolder = "site";

    public async Task<VillageInfoInputModel> GetVillageInfoAsync(CancellationToken cancellationToken = default)
    {
        var village = await db.VillageInfo.AsNoTracking().SingleAsync(cancellationToken);
        return new VillageInfoInputModel
        {
            Name = village.Name,
            NameGu = village.NameGu,
            ShortIntro = village.ShortIntro,
            ShortIntroGu = village.ShortIntroGu,
            Population = village.Population,
            Households = village.Households,
            AreaSqKm = village.AreaSqKm,
            StatsSource = village.StatsSource,
            StatsSourceGu = village.StatsSourceGu,
            Taluka = village.Taluka,
            TalukaGu = village.TalukaGu,
            District = village.District,
            DistrictGu = village.DistrictGu,
            State = village.State,
            StateGu = village.StateGu,
            PinCode = village.PinCode,
            Latitude = village.Latitude,
            Longitude = village.Longitude,
            MapZoom = village.MapZoom
        };
    }

    public async Task SaveVillageInfoAsync(VillageInfoInputModel input, CancellationToken cancellationToken = default)
    {
        var village = await db.VillageInfo.SingleAsync(cancellationToken);
        village.Name = input.Name.Trim();
        village.NameGu = Clean(input.NameGu);
        village.ShortIntro = input.ShortIntro.Trim();
        village.ShortIntroGu = Clean(input.ShortIntroGu);
        village.Population = input.Population;
        village.Households = input.Households;
        village.AreaSqKm = input.AreaSqKm;
        village.StatsSource = Clean(input.StatsSource);
        village.StatsSourceGu = Clean(input.StatsSourceGu);
        village.Taluka = Clean(input.Taluka);
        village.TalukaGu = Clean(input.TalukaGu);
        village.District = Clean(input.District);
        village.DistrictGu = Clean(input.DistrictGu);
        village.State = Clean(input.State);
        village.StateGu = Clean(input.StateGu);
        village.PinCode = Clean(input.PinCode);
        village.Latitude = input.Latitude is { } latitude ? Math.Round(latitude, 6) : null;
        village.Longitude = input.Longitude is { } longitude ? Math.Round(longitude, 6) : null;
        village.MapZoom = input.MapZoom;

        await db.SaveChangesAsync(cancellationToken);
        siteInfo.Invalidate();
    }

    public async Task<SiteSettingsInputModel> GetSiteSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.SiteSettings.AsNoTracking().SingleAsync(cancellationToken);
        return new SiteSettingsInputModel
        {
            SiteName = settings.SiteName,
            SiteNameGu = settings.SiteNameGu,
            Tagline = settings.Tagline,
            TaglineGu = settings.TaglineGu,
            OfficeEmail = settings.OfficeEmail,
            OfficePhone = settings.OfficePhone,
            ShowOfficePhone = settings.ShowOfficePhone,
            OfficeAddress = settings.OfficeAddress,
            OfficeAddressGu = settings.OfficeAddressGu,
            FacebookUrl = settings.FacebookUrl,
            InstagramUrl = settings.InstagramUrl,
            YouTubeUrl = settings.YouTubeUrl,
            WhatsAppUrl = settings.WhatsAppUrl,
            DefaultMetaDescription = settings.DefaultMetaDescription,
            DefaultMetaDescriptionGu = settings.DefaultMetaDescriptionGu,
            CurrentHeroImageUrl = MediaUrls.Image(settings.HeroImagePath),
            CurrentHeroVideoUrl = string.IsNullOrWhiteSpace(settings.HeroVideoPath) ? null : $"{MediaUrls.RequestPath}/{settings.HeroVideoPath}"
        };
    }

    public async Task<OperationResult> SaveSiteSettingsAsync(SiteSettingsInputModel input, CancellationToken cancellationToken = default)
    {
        var settings = await db.SiteSettings.SingleAsync(cancellationToken);
        var result = new OperationResult();

        // 1. Store new files first: if a file is refused, nothing else changes.
        string? newImage = null, newVideo = null;
        if (input.HeroImage is { Length: > 0 })
        {
            var upload = await images.SaveAsync(input.HeroImage, MediaFolder, cancellationToken);
            if (upload.Succeeded)
            {
                newImage = upload.Image!.Path;
            }
            else
            {
                result.AddError(nameof(input.HeroImage), upload.Error!);
            }
        }

        if (input.HeroVideo is { Length: > 0 })
        {
            var upload = await videos.SaveAsync(input.HeroVideo, MediaFolder, cancellationToken);
            if (upload.Succeeded)
            {
                newVideo = upload.Path;
            }
            else
            {
                result.AddError(nameof(input.HeroVideo), upload.Error!);
            }
        }

        if (!result.Succeeded)
        {
            images.Delete(newImage);
            videos.Delete(newVideo);
            return result;
        }

        // 2. Update the row.
        settings.SiteName = input.SiteName.Trim();
        settings.SiteNameGu = Clean(input.SiteNameGu);
        settings.Tagline = input.Tagline.Trim();
        settings.TaglineGu = Clean(input.TaglineGu);
        settings.OfficeEmail = Clean(input.OfficeEmail);
        settings.OfficePhone = Clean(input.OfficePhone);
        settings.ShowOfficePhone = input.ShowOfficePhone;
        settings.OfficeAddress = Clean(input.OfficeAddress);
        settings.OfficeAddressGu = Clean(input.OfficeAddressGu);
        settings.FacebookUrl = Clean(input.FacebookUrl);
        settings.InstagramUrl = Clean(input.InstagramUrl);
        settings.YouTubeUrl = Clean(input.YouTubeUrl);
        settings.WhatsAppUrl = Clean(input.WhatsAppUrl);
        settings.DefaultMetaDescription = Clean(input.DefaultMetaDescription);
        settings.DefaultMetaDescriptionGu = Clean(input.DefaultMetaDescriptionGu);

        string? replacedImage = null, replacedVideo = null;
        if (newImage is not null || input.RemoveHeroImage)
        {
            replacedImage = settings.HeroImagePath;
            settings.HeroImagePath = newImage;
        }

        if (newVideo is not null || input.RemoveHeroVideo)
        {
            replacedVideo = settings.HeroVideoPath;
            settings.HeroVideoPath = newVideo;
        }

        // 3. Save, then delete replaced files. If saving fails, delete the new files instead, so the database
        //    never points at a missing file and no unused file is left behind.
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            images.Delete(newImage);
            videos.Delete(newVideo);
            throw;
        }

        images.Delete(replacedImage);
        videos.Delete(replacedVideo);
        siteInfo.Invalidate();
        return result;
    }

    /// <summary>Trimmed text, or null for an empty field (so "Gujarati, else English" falls back correctly).</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
