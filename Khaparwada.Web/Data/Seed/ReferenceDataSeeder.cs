using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Data.Seed;

/// <summary>
/// Inserts the data the site cannot work without: the three category lists and the two settings rows.
/// Each table is filled only while it is completely empty, so a category an admin deletes later is not
/// brought back on the next restart. Unknown village facts are clearly marked placeholders, and the
/// Gujarati names are a first draft for a native speaker to check.
/// </summary>
public sealed class ReferenceDataSeeder(AppDbContext db, ILogger<ReferenceDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!await db.PlaceCategories.AnyAsync(cancellationToken))
        {
            db.PlaceCategories.AddRange(
                NewPlaceCategory(1, "Temples", "મંદિરો", "temples", "fa-solid fa-gopuram", "#8C1C3A", showInStats: true),
                NewPlaceCategory(2, "Schools", "શાળાઓ", "schools", "fa-solid fa-school", "#23395B", showInStats: true),
                NewPlaceCategory(3, "Government Offices", "સરકારી કચેરીઓ", "government-offices", "fa-solid fa-landmark", "#5B6770"),
                NewPlaceCategory(4, "Hospitals & Health Centers", "દવાખાનાં અને આરોગ્ય કેન્દ્રો", "health-centers", "fa-solid fa-hospital", "#1F7A7A"),
                NewPlaceCategory(5, "Water Places", "જળાશયો", "water-places", "fa-solid fa-water", "#2A6FB0"),
                NewPlaceCategory(6, "Historical Places", "ઐતિહાસિક સ્થળો", "historical-places", "fa-solid fa-monument", "#B4532A"),
                NewPlaceCategory(7, "Shops & Markets", "દુકાનો અને બજાર", "shops-markets", "fa-solid fa-store", "#9A6A0F"),
                NewPlaceCategory(8, "Other Important Places", "અન્ય મહત્ત્વનાં સ્થળો", "other-places", "fa-solid fa-location-dot", "#6B3E75"));
        }

        if (!await db.PersonCategories.AnyAsync(cancellationToken))
        {
            db.PersonCategories.AddRange(
                NewPersonCategory(1, "Teachers", "શિક્ષકો", "teachers", "fa-solid fa-chalkboard-user"),
                NewPersonCategory(2, "Doctors", "ડૉક્ટરો", "doctors", "fa-solid fa-user-doctor"),
                NewPersonCategory(3, "Engineers", "ઇજનેરો", "engineers", "fa-solid fa-helmet-safety"),
                NewPersonCategory(4, "Government Employees", "સરકારી કર્મચારીઓ", "government-employees", "fa-solid fa-building-columns"),
                NewPersonCategory(5, "Business Owners", "વેપારીઓ", "business-owners", "fa-solid fa-briefcase"),
                NewPersonCategory(6, "Farmers", "ખેડૂતો", "farmers", "fa-solid fa-tractor"),
                NewPersonCategory(7, "Sports Persons", "ખેલાડીઓ", "sports-persons", "fa-solid fa-medal"),
                NewPersonCategory(8, "Social Workers", "સામાજિક કાર્યકરો", "social-workers", "fa-solid fa-hand-holding-heart"),
                NewPersonCategory(9, "Other Contributors", "અન્ય યોગદાનકર્તાઓ", "other-contributors", "fa-solid fa-star"));
        }

        if (!await db.GalleryCategories.AnyAsync(cancellationToken))
        {
            db.GalleryCategories.AddRange(
                // Gujarati left empty until the Gujarati spelling of the village name is confirmed.
                NewGalleryCategory(1, "Old Khaparwada", null, "old-khaparwada"),
                NewGalleryCategory(2, "Festivals", "તહેવારો", "festivals"),
                NewGalleryCategory(3, "Navratri", "નવરાત્રી", "navratri"),
                NewGalleryCategory(4, "Diwali", "દિવાળી", "diwali"),
                NewGalleryCategory(5, "Village Events", "ગામના કાર્યક્રમો", "village-events"),
                NewGalleryCategory(6, "Sports", "રમતગમત", "sports"),
                NewGalleryCategory(7, "Nature", "પ્રકૃતિ", "nature"),
                NewGalleryCategory(8, "Community", "સમુદાય", "community"),
                NewGalleryCategory(9, "Other", "અન્ય", "other"));
        }

        if (!await db.VillageInfo.AnyAsync(cancellationToken))
        {
            db.VillageInfo.Add(new VillageInfo
            {
                Id = VillageInfo.SingletonId,
                Name = "Khaparwada",
                ShortIntro = "[Add a short introduction to Khaparwada here]",
                ShortIntroGu = "[અહીં ગામનો ટૂંકો પરિચય ઉમેરો]",
                Taluka = "[Add taluka]",
                District = "[Add district]",
                State = "[Add state]"
            });
        }

        if (!await db.SiteSettings.AnyAsync(cancellationToken))
        {
            db.SiteSettings.Add(new SiteSetting
            {
                Id = SiteSetting.SingletonId,
                SiteName = "Khaparwada Connect",
                Tagline = "Our Village • Our People • Our Heritage • Our Future",
                TaglineGu = "આપણું ગામ • આપણા લોકો • આપણો વારસો • આપણું ભવિષ્ય",
                DefaultMetaDescription =
                    "Khaparwada Connect is the digital home of Khaparwada village: its places, people, events, notices, photos and heritage."
            });
        }

        var inserted = await db.SaveChangesAsync(cancellationToken);
        if (inserted > 0)
        {
            logger.LogInformation("Inserted {Count} rows of reference data.", inserted);
        }
    }

    private static PlaceCategory NewPlaceCategory(
        int sortOrder, string name, string nameGu, string slug, string iconClass, string markerColor, bool showInStats = false) =>
        new()
        {
            Name = name,
            NameGu = nameGu,
            Slug = slug,
            IconClass = iconClass,
            MarkerColor = markerColor,
            ShowInStats = showInStats,
            SortOrder = sortOrder
        };

    private static PersonCategory NewPersonCategory(int sortOrder, string name, string nameGu, string slug, string iconClass) =>
        new()
        {
            Name = name,
            NameGu = nameGu,
            Slug = slug,
            IconClass = iconClass,
            SortOrder = sortOrder
        };

    private static GalleryCategory NewGalleryCategory(int sortOrder, string name, string? nameGu, string slug) =>
        new()
        {
            Name = name,
            NameGu = nameGu,
            Slug = slug,
            SortOrder = sortOrder
        };
}
