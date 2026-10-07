using Khaparwada.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Data.Seed;

/// <summary>
/// Inserts the data the site cannot work without: the three category lists, the two settings rows and the
/// About page's sections. Each table is filled only while it is completely empty, so a row an admin deletes
/// later is not brought back on the next restart. Unknown village facts are clearly marked placeholders, and
/// the Gujarati names are a first draft for a native speaker to check.
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
                Taluka = KnownFacts.Taluka,
                TalukaGu = KnownFacts.TalukaGu,
                District = KnownFacts.District,
                DistrictGu = KnownFacts.DistrictGu,
                State = KnownFacts.State,
                StateGu = KnownFacts.StateGu
            });
        }
        else
        {
            await FillKnownFactsAsync(cancellationToken);
        }

        // The About page's topics (spec: introduction, history, culture, traditions, festivals, geography,
        // development; milestones come from the timeline). They are the page's structure, so every
        // environment gets them, as clearly marked placeholders for the admin to replace.
        if (!await db.AboutSections.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            AddAboutSections();
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
            logger.LogInformation("Saved {Count} rows of reference data.", inserted);
        }
    }

    /// <summary>
    /// Databases created before these facts were known still hold "[Add taluka]" and so on. A value is replaced
    /// only while it is still that original placeholder, so anything an admin typed is never overwritten.
    /// </summary>
    private async Task FillKnownFactsAsync(CancellationToken cancellationToken)
    {
        var village = await db.VillageInfo.SingleAsync(cancellationToken);
        if (village.Taluka == "[Add taluka]")
        {
            village.Taluka = KnownFacts.Taluka;
            village.TalukaGu ??= KnownFacts.TalukaGu;
        }

        if (village.District == "[Add district]")
        {
            village.District = KnownFacts.District;
            village.DistrictGu ??= KnownFacts.DistrictGu;
        }

        if (village.State == "[Add state]")
        {
            village.State = KnownFacts.State;
            village.StateGu ??= KnownFacts.StateGu;
        }
    }

    private void AddAboutSections()
    {
        (string Slug, string Title, string TitleGu, string Body, string BodyGu)[] sections =
        [
            ("introduction", "Introduction", "પરિચય",
                "[Add an introduction to Khaparwada here: where it is, who lives here and what makes it special.]",
                "[અહીં ગામનો પરિચય ઉમેરો: ગામ ક્યાં આવેલું છે, અહીં કોણ રહે છે અને ગામની વિશેષતા શું છે.]"),
            ("history", "History", "ઇતિહાસ",
                "[Add Khaparwada historical information here]",
                "[અહીં ગામની ઐતિહાસિક માહિતી ઉમેરો]"),
            ("culture", "Culture", "સંસ્કૃતિ",
                "[Add information about Khaparwada's culture here]",
                "[અહીં ગામની સંસ્કૃતિ વિશે માહિતી ઉમેરો]"),
            ("traditions", "Traditions", "પરંપરાઓ",
                "[Add the village's traditions here]",
                "[અહીં ગામની પરંપરાઓ ઉમેરો]"),
            ("festivals", "Festivals", "તહેવારો",
                "[Add how festivals are celebrated in Khaparwada here]",
                "[અહીં ગામમાં તહેવારો કેવી રીતે ઉજવાય છે તે ઉમેરો]"),
            ("geography", "Geography", "ભૂગોળ",
                "[Add the village's geography here: land, water sources, climate, nearby villages]",
                "[અહીં ગામની ભૂગોળ ઉમેરો: જમીન, પાણીના સ્રોત, આબોહવા, આસપાસનાં ગામ]"),
            ("development", "Village Development", "ગામનો વિકાસ",
                "[Add development work and future plans here]",
                "[અહીં વિકાસનાં કામો અને ભવિષ્યની યોજનાઓ ઉમેરો]")
        ];

        db.AboutSections.AddRange(sections.Select((s, index) => new AboutSection
        {
            Slug = s.Slug,
            Title = s.Title,
            TitleGu = s.TitleGu,
            Body = s.Body,
            BodyGu = s.BodyGu,
            SortOrder = (index + 1) * 10,
            IsPublished = true
        }));
    }

    /// <summary>Confirmed by the village (October 2026). Gujarati spellings are the official names; please confirm.</summary>
    private static class KnownFacts
    {
        public const string Taluka = "Gandevi";
        public const string TalukaGu = "ગણદેવી";
        public const string District = "Navsari";
        public const string DistrictGu = "નવસારી";
        public const string State = "Gujarat";
        public const string StateGu = "ગુજરાત";
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
