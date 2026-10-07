using Khaparwada.Web.Infrastructure.Options;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Data.Seed;

/// <summary>
/// Development-only sample content, so every page has something to show while it is being built.
/// Everything is marked "[Sample]" / "[નમૂનો]" and states no facts about Khaparwada; map coordinates are
/// around a demo point in Gujarat, not the village's real location.
/// Runs only when the Places table has never had a row (deleted ones included), so it never mixes
/// with real content. Delete the samples in Admin once real content is in.
/// </summary>
public sealed class DemoDataSeeder(
    AppDbContext db,
    TimeProvider timeProvider,
    IOptions<SiteOptions> siteOptions,
    ILogger<DemoDataSeeder> logger)
{
    private const decimal DemoLatitude = 22.2587m;
    private const decimal DemoLongitude = 71.1924m;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Places.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(siteOptions.Value.TimeZoneId);
        var clock = new LocalClock(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).Date, timeZone);

        var placeCategories = await db.PlaceCategories.ToDictionaryAsync(c => c.Slug, cancellationToken);
        var personCategories = await db.PersonCategories.ToDictionaryAsync(c => c.Slug, cancellationToken);
        var galleryCategories = await db.GalleryCategories.ToDictionaryAsync(c => c.Slug, cancellationToken);

        AddHistory(clock);
        AddThenNow();
        var places = AddPlaces(placeCategories);
        AddPeople(personCategories);
        var events = AddEvents(places, clock);
        AddAlbums(galleryCategories, events);
        AddNotices(clock);
        AddMemories(clock);
        AddUsefulLinks();
        AddContactMessage();

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Inserted [Sample] demo content (Seed:DemoData = true).");
    }

    private void AddHistory(LocalClock clock)
    {
        HistoryEntry NewEntry(int sortYear, string yearLabel, string yearLabelGu, string title, string titleGu, bool highlighted) =>
            new()
            {
                SortYear = sortYear,
                YearLabel = yearLabel,
                YearLabelGu = yearLabelGu,
                Title = title,
                TitleGu = titleGu,
                Description = "[Add Khaparwada historical information here]",
                DescriptionGu = "[અહીં ગામની ઐતિહાસિક માહિતી ઉમેરો]",
                IsHighlighted = highlighted,
                IsPublished = true
            };

        // SortYear only orders the samples; visitors see "[Year]" until real years are entered.
        db.HistoryEntries.AddRange(
            NewEntry(1900, "[Year]", "[વર્ષ]", "[Sample] Early history", "[નમૂનો] પ્રારંભિક ઇતિહાસ", highlighted: false),
            NewEntry(1950, "[Year]", "[વર્ષ]", "[Sample] An important milestone", "[નમૂનો] એક મહત્ત્વનું સીમાચિહ્ન", highlighted: true),
            NewEntry(1980, "[Year]", "[વર્ષ]", "[Sample] A development milestone", "[નમૂનો] વિકાસનું સીમાચિહ્ન", highlighted: true),
            NewEntry(2010, "[Year]", "[વર્ષ]", "[Sample] A recent milestone", "[નમૂનો] તાજેતરનું સીમાચિહ્ન", highlighted: false),
            NewEntry(clock.Today.Year, "Present", "વર્તમાન", "[Sample] Khaparwada today", "[નમૂનો] આજનું ગામ", highlighted: true));
    }

    private void AddThenNow()
    {
        ThenNowComparison NewComparison(int sortOrder, string title, string titleGu) =>
            new()
            {
                Title = title,
                TitleGu = titleGu,
                Caption = "[Add the story behind these two photos]",
                CaptionGu = "[આ બે ફોટા પાછળની વાત ઉમેરો]",
                ThenLabel = "Then",
                ThenLabelGu = "પહેલાં",
                NowLabel = "Now",
                NowLabelGu = "હવે",
                SortOrder = sortOrder,
                IsPublished = true
            };

        db.ThenNowComparisons.AddRange(
            NewComparison(1, "[Sample] Village square", "[નમૂનો] ગામનો ચોક"),
            NewComparison(2, "[Sample] Main road", "[નમૂનો] મુખ્ય રસ્તો"));
    }

    private Dictionary<string, Place> AddPlaces(Dictionary<string, PlaceCategory> categories)
    {
        Place NewPlace(
            string categorySlug, string slug, string name, string nameGu,
            decimal latitudeOffset, decimal longitudeOffset, bool featured,
            string? timings = null, string? timingsGu = null) =>
            new()
            {
                Category = categories[categorySlug],
                Slug = slug,
                Name = name,
                NameGu = nameGu,
                Summary = "[Sample] Replace with a short description of this place.",
                SummaryGu = "[નમૂનો] અહીં આ સ્થળનું ટૂંકું વર્ણન લખો.",
                Description = "[Add this place's history, importance and details here]",
                DescriptionGu = "[અહીં આ સ્થળનો ઇતિહાસ, મહત્ત્વ અને વિગતો ઉમેરો]",
                Address = "[Sample] Add the address",
                AddressGu = "[નમૂનો] સરનામું ઉમેરો",
                Latitude = DemoLatitude + latitudeOffset,
                Longitude = DemoLongitude + longitudeOffset,
                Timings = timings,
                TimingsGu = timingsGu,
                IsFeatured = featured,
                IsPublished = true
            };

        Place[] places =
        [
            NewPlace("temples", "sample-village-temple", "[Sample] Village Temple", "[નમૂનો] ગામનું મંદિર",
                0.0012m, -0.0008m, featured: true,
                "[Sample] Darshan: 6:00 AM – 12:00 PM, 4:00 PM – 9:00 PM",
                "[નમૂનો] દર્શન: સવારે 6:00 – બપોરે 12:00, સાંજે 4:00 – રાત્રે 9:00"),
            NewPlace("schools", "sample-primary-school", "[Sample] Primary School", "[નમૂનો] પ્રાથમિક શાળા",
                -0.0010m, 0.0015m, featured: true),
            NewPlace("government-offices", "sample-gram-panchayat-office", "[Sample] Gram Panchayat Office", "[નમૂનો] ગ્રામ પંચાયત કચેરી",
                0.0004m, 0.0006m, featured: true,
                "[Sample] Add office hours", "[નમૂનો] કચેરીનો સમય ઉમેરો"),
            NewPlace("health-centers", "sample-health-centre", "[Sample] Primary Health Centre", "[નમૂનો] પ્રાથમિક આરોગ્ય કેન્દ્ર",
                -0.0018m, -0.0012m, featured: false),
            NewPlace("water-places", "sample-village-lake", "[Sample] Village Lake", "[નમૂનો] ગામનું તળાવ",
                0.0030m, 0.0025m, featured: true),
            NewPlace("historical-places", "sample-heritage-site", "[Sample] Heritage Site", "[નમૂનો] ઐતિહાસિક સ્થળ",
                0.0022m, -0.0030m, featured: false),
            NewPlace("shops-markets", "sample-main-bazaar", "[Sample] Main Bazaar", "[નમૂનો] મુખ્ય બજાર",
                -0.0005m, -0.0002m, featured: false),
            NewPlace("other-places", "sample-bus-stand", "[Sample] Bus Stand", "[નમૂનો] બસ સ્ટેન્ડ",
                -0.0028m, 0.0009m, featured: false)
        ];

        db.Places.AddRange(places);
        return places.ToDictionary(p => p.Slug);
    }

    private void AddPeople(Dictionary<string, PersonCategory> categories)
    {
        Person NewPerson(
            string categorySlug, string slug, string fullName, string fullNameGu,
            string profession, string professionGu, ModerationStatus status, bool featured) =>
            new()
            {
                Category = categories[categorySlug],
                Slug = slug,
                FullName = fullName,
                FullNameGu = fullNameGu,
                Profession = profession,
                ProfessionGu = professionGu,
                ShortBio = "[Sample] Add a short biography here, only with this person's consent.",
                ShortBioGu = "[નમૂનો] અહીં ટૂંકો પરિચય લખો, ફક્ત તે વ્યક્તિની સંમતિથી.",
                Achievement = "[Sample] Add an achievement",
                AchievementGu = "[નમૂનો] સિદ્ધિ ઉમેરો",
                Contribution = "[Sample] Add their contribution to the village",
                ContributionGu = "[નમૂનો] ગામ માટેનું યોગદાન ઉમેરો",
                HasConsent = status == ModerationStatus.Published,
                ConsentNote = status == ModerationStatus.Published ? "[Sample] Demo profile: no real person" : null,
                ConsentRecordedAt = status == ModerationStatus.Published ? timeProvider.GetUtcNow() : null,
                Status = status,
                IsFeatured = featured
            };

        db.People.AddRange(
            NewPerson("teachers", "sample-teacher", "[Sample] Teacher Profile", "[નમૂનો] શિક્ષક પ્રોફાઇલ",
                "Teacher", "શિક્ષક", ModerationStatus.Published, featured: true),
            NewPerson("doctors", "sample-doctor", "[Sample] Doctor Profile", "[નમૂનો] ડૉક્ટર પ્રોફાઇલ",
                "Doctor", "ડૉક્ટર", ModerationStatus.Published, featured: true),
            NewPerson("farmers", "sample-farmer", "[Sample] Farmer Profile", "[નમૂનો] ખેડૂત પ્રોફાઇલ",
                "Farmer", "ખેડૂત", ModerationStatus.Published, featured: true),
            NewPerson("sports-persons", "sample-sportsperson", "[Sample] Sportsperson Profile", "[નમૂનો] ખેલાડી પ્રોફાઇલ",
                "Athlete", "ખેલાડી", ModerationStatus.Published, featured: false),
            // Waits in the approval queue: no consent recorded yet, so it cannot be approved.
            NewPerson("social-workers", "sample-social-worker", "[Sample] Social Worker Profile", "[નમૂનો] સામાજિક કાર્યકર પ્રોફાઇલ",
                "Social Worker", "સામાજિક કાર્યકર", ModerationStatus.Pending, featured: false));
    }

    private Dictionary<string, Event> AddEvents(Dictionary<string, Place> places, LocalClock clock)
    {
        const string summary = "[Sample] Replace with a short description of the event.";
        const string summaryGu = "[નમૂનો] અહીં કાર્યક્રમનું ટૂંકું વર્ણન લખો.";

        Event[] events =
        [
            new()
            {
                Slug = "sample-navratri-garba-night",
                Title = "[Sample] Navratri Garba Night",
                TitleGu = "[નમૂનો] નવરાત્રી ગરબા રાત્રિ",
                Summary = summary,
                SummaryGu = summaryGu,
                StartsAt = clock.At(10, 21),
                EndsAt = clock.At(11, 0, 30),
                Place = places["sample-village-temple"],
                IsFeatured = true,
                IsPublished = true
            },
            new()
            {
                Slug = "sample-gram-sabha-meeting",
                Title = "[Sample] Gram Sabha Meeting",
                TitleGu = "[નમૂનો] ગ્રામ સભા બેઠક",
                Summary = summary,
                SummaryGu = summaryGu,
                StartsAt = clock.At(20, 11),
                EndsAt = clock.At(20, 13),
                Place = places["sample-gram-panchayat-office"],
                IsPublished = true
            },
            new()
            {
                Slug = "sample-diwali-celebration",
                Title = "[Sample] Diwali Celebration",
                TitleGu = "[નમૂનો] દિવાળી ઉજવણી",
                Summary = summary,
                SummaryGu = summaryGu,
                StartsAt = clock.At(30, 0),
                IsAllDay = true,
                LocationText = "[Sample] Village square",
                LocationTextGu = "[નમૂનો] ગામનો ચોક",
                IsFeatured = true,
                IsPublished = true
            },
            new()
            {
                Slug = "sample-cleanliness-drive",
                Title = "[Sample] Village Cleanliness Drive",
                TitleGu = "[નમૂનો] ગામ સફાઈ અભિયાન",
                Summary = summary,
                SummaryGu = summaryGu,
                StartsAt = clock.At(-15, 8),
                EndsAt = clock.At(-15, 11),
                LocationText = "[Sample] Whole village",
                LocationTextGu = "[નમૂનો] આખું ગામ",
                IsPublished = true
            }
        ];

        db.Events.AddRange(events);
        return events.ToDictionary(e => e.Slug);
    }

    private void AddAlbums(Dictionary<string, GalleryCategory> categories, Dictionary<string, Event> events)
    {
        const string description = "[Sample] Add a description of these photos";
        const string descriptionGu = "[નમૂનો] આ ફોટાનું વર્ણન ઉમેરો";
        var cleanlinessDrive = events["sample-cleanliness-drive"];

        db.GalleryAlbums.AddRange(
            new GalleryAlbum
            {
                Category = categories["navratri"],
                Slug = "sample-navratri-photos",
                Title = "[Sample] Navratri Photos",
                TitleGu = "[નમૂનો] નવરાત્રીના ફોટા",
                Description = description,
                DescriptionGu = descriptionGu,
                IsPublished = true
            },
            new GalleryAlbum
            {
                Category = categories["old-khaparwada"],
                Slug = "sample-old-village-photos",
                Title = "[Sample] Old Village Photos",
                TitleGu = "[નમૂનો] જૂના ગામના ફોટા",
                Description = description,
                DescriptionGu = descriptionGu,
                IsPublished = true
            },
            new GalleryAlbum
            {
                Category = categories["village-events"],
                Event = cleanlinessDrive,
                Slug = "sample-cleanliness-drive-photos",
                Title = "[Sample] Cleanliness Drive Photos",
                TitleGu = "[નમૂનો] સફાઈ અભિયાનના ફોટા",
                Description = description,
                DescriptionGu = descriptionGu,
                TakenOn = DateOnly.FromDateTime(cleanlinessDrive.StartsAt.DateTime),
                IsPublished = true
            });
    }

    private void AddNotices(LocalClock clock)
    {
        const string description = "[Sample] Replace with the full text of the notice.";
        const string descriptionGu = "[નમૂનો] અહીં સૂચનાની સંપૂર્ણ વિગત લખો.";

        Notice NewNotice(
            string slug, string title, string titleGu, NoticeCategory category, NoticePriority priority,
            DateTimeOffset publishedAt, DateTimeOffset? expiresAt) =>
            new()
            {
                Slug = slug,
                Title = title,
                TitleGu = titleGu,
                Description = description,
                DescriptionGu = descriptionGu,
                Category = category,
                Priority = priority,
                PublishedAt = publishedAt,
                ExpiresAt = expiresAt,
                IsPublished = true
            };

        db.Notices.AddRange(
            NewNotice("sample-water-supply-timing", "[Sample] Water supply timing update", "[નમૂનો] પાણી પુરવઠાના સમયમાં ફેરફાર",
                NoticeCategory.Water, NoticePriority.Important, clock.At(-1, 9), clock.At(14, 23, 59)),
            NewNotice("sample-gram-sabha-notice", "[Sample] Gram Sabha meeting notice", "[નમૂનો] ગ્રામ સભા બેઠકની સૂચના",
                NoticeCategory.Meeting, NoticePriority.Normal, clock.At(-2, 10), clock.At(20, 13)),
            NewNotice("sample-government-scheme", "[Sample] Government scheme information", "[નમૂનો] સરકારી યોજનાની માહિતી",
                NoticeCategory.GovernmentScheme, NoticePriority.Normal, clock.At(-5, 10), expiresAt: null),
            // Urgent and active: shows the site-wide alert bar once the layout is built (Phase 2).
            NewNotice("sample-emergency-notice", "[Sample] Emergency notice example", "[નમૂનો] તાત્કાલિક સૂચનાનું ઉદાહરણ",
                NoticeCategory.Emergency, NoticePriority.Urgent, clock.At(-1, 8), clock.At(2, 23, 59)),
            // Already expired: must not appear anywhere on the public site.
            NewNotice("sample-expired-notice", "[Sample] Expired notice (no longer shown)", "[નમૂનો] સમાપ્ત થયેલી સૂચના",
                NoticeCategory.Electricity, NoticePriority.Normal, clock.At(-20, 10), clock.At(-5, 18)));
    }

    private void AddMemories(LocalClock clock)
    {
        db.MemorySubmissions.AddRange(
            new MemorySubmission
            {
                Title = "[Sample] A memory from the old days",
                Story = "[Sample] Residents share memories, stories and old photos from the Memories page. " +
                        "An admin reviews each one before it appears here.",
                EraLabel = "[Era]",
                SubmitterName = "[Sample] Resident",
                ConsentToPublish = true,
                Status = ModerationStatus.Published,
                ReviewedAt = clock.At(-3, 10),
                PublishedAt = clock.At(-3, 10),
                Slug = "sample-memory"
            },
            new MemorySubmission
            {
                Title = "[Sample] An approved memory, not yet published",
                Story = "[Sample] Approved by an admin and waiting to be published.",
                SubmitterName = "[Sample] Resident",
                ConsentToPublish = true,
                Status = ModerationStatus.Approved,
                ReviewedAt = clock.At(-1, 16)
            },
            // Residents write in their own language; a Gujarati submission waiting for review.
            new MemorySubmission
            {
                Title = "[નમૂનો] ગામની જૂની યાદ",
                Story = "[નમૂનો] ગામલોકો અહીં પોતાની યાદો અને વાર્તાઓ ગુજરાતીમાં પણ મોકલી શકે છે. " +
                        "પ્રકાશિત થતાં પહેલાં એડમિન તેની સમીક્ષા કરે છે.",
                SubmitterName = "[નમૂનો] ગામવાસી",
                SubmitterContact = "[Sample] phone or email (admin-only)",
                ConsentToPublish = true,
                Status = ModerationStatus.Pending
            });
    }

    private void AddUsefulLinks()
    {
        db.UsefulLinks.AddRange(
            new UsefulLink
            {
                Title = "National Portal of India",
                TitleGu = "ભારતનું રાષ્ટ્રીય પોર્ટલ",
                Url = "https://www.india.gov.in",
                ShowInFooter = true,
                SortOrder = 1
            },
            new UsefulLink
            {
                Title = "Digital Gujarat",
                TitleGu = "ડિજિટલ ગુજરાત",
                Url = "https://www.digitalgujarat.gov.in",
                ShowInFooter = true,
                SortOrder = 2
            });
    }

    private void AddContactMessage()
    {
        db.ContactMessages.Add(new ContactMessage
        {
            Name = "[Sample] Visitor",
            Email = "visitor@example.com",
            Subject = "[Sample] Question about the Navratri event",
            Message = "[Sample] This is an example message from the Contact form. Only admins can read it."
        });
    }

    /// <summary>Village-local date and time relative to today, so "upcoming" samples stay in the future.</summary>
    private sealed record LocalClock(DateTime Today, TimeZoneInfo TimeZone)
    {
        public DateTimeOffset At(int daysFromToday, int hour, int minute = 0)
        {
            var local = Today.AddDays(daysFromToday).AddHours(hour).AddMinutes(minute);
            return new DateTimeOffset(local, TimeZone.GetUtcOffset(local));
        }
    }
}
