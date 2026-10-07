# Khaparwada Connect — Architecture

Village portal for Khaparwada. Tagline: **"Our Village • Our People • Our Heritage • Our Future"**.

This is the reference for every development phase. When a decision changes, update this file in the
same commit.

## 1. Stack

| Area | Choice |
|---|---|
| Framework | ASP.NET Core MVC on **.NET 10** (LTS, supported until November 2028) |
| Data | EF Core **10.0.12** + SQL Server (development: SQL Server 2025 Express at `.\SQLEXPRESS`) |
| Sign-in | ASP.NET Core Identity, roles **Admin** and **Editor**, no public registration |
| UI | Razor views, Bootstrap 5.3.8 re-themed through CSS variables, small plain-JS files; jQuery only on pages with forms |
| Icons | Font Awesome Free 7.3.1 icons, delivered as one SVG sprite (`wwwroot/images/icons.svg`) |
| Fonts | Rasa (headings) and Mukta Vaani (text), self-hosted, each with Gujarati and Latin letters |
| Map | Leaflet 1.9.4 + OpenStreetMap tiles, tile URL in configuration, loaded only when a map scrolls into view |
| Images | SkiaSharp 3.119 (MIT, maintained by Microsoft): check, turn upright, convert colours, resize, WebP without metadata |
| Tools | Visual Studio 2026, SSMS 22, `dotnet-ef` pinned in `dotnet-tools.json` |

## 2. How a request flows

```
Browser (phone / desktop)
        │ HTTPS
        ▼
Kestrel (IIS in production) → Program.cs middleware, in this order:
  error page / HSTS → friendly status pages (/error/404) → HTTPS redirect
  → /uploads/… (uploaded files: .webp .mp4 .pdf only, cached a year, nosniff)
  → language: "/gu/…" becomes PathBase "/gu" + path "/…"  (LanguagePrefixMiddleware; "/gu/admin" → 404)
  → routing → request culture from the URL → rate limiting (sign-in) → authentication → authorization
  → static assets (fingerprinted, pre-compressed) and controllers
  Later phases add: security headers, output caching.
        │
        ├── Public controllers       /places/{slug}   /gu/places/{slug}   …
        └── Admin area controllers   /admin/…   policy "Staff" (Admin or Editor); settings screens "AdminOnly"
                 │  thin: call a service → return a view
                 ▼
        Services (one per feature) — public queries all filtered by Services/Common/PublicVisibility.cs
           │                         │                          │
           ▼                         ▼                          ▼
     AppDbContext (EF Core 10)   Media: image + video        IMemoryCache (site settings 10 min,
           │                     upload services             urgent notices 1 min; admin saves clear it)
           ▼
   SQL Server: KhaparwadaConnect
```

Views receive ViewModels only; forms bind to InputModels. Entities never reach a view or a form.

## 3. Key decisions and the problem each one prevents

| Decision | Problem it prevents |
|---|---|
| One web project, organised by feature folders (+ a test project in Phase 14) | Multi-project "Clean Architecture" ceremony that costs more than it protects for one UI and one developer |
| No repository layer; services use `AppDbContext` directly | A generic repository hides `Include` / `AsNoTracking` / projections → slow queries or a leaky abstraction |
| ViewModels for display, InputModels for forms, mapped by hand | Over-posting: a crafted POST setting `Status=Published` on a resident's submission |
| Seed data inserted at startup "only if missing", never EF `HasData` | `HasData` lets a later migration overwrite the admin's edits in production |
| Windows authentication + `TrustServerCertificate=True` (local only) | No password exists to leak; SqlClient encrypts by default and Express uses a self-signed certificate |
| EF SQL compatibility level 150 (SQL Server 2019) | SQL that works on the 2025 dev server but fails on older hosting |
| Gujarati "twin" columns (`Title` + `TitleGu`), Gujarati falls back to English | Retrofitting Gujarati into every table, form and query in Phase 12 |
| Timestamps and soft delete applied centrally (interceptor + query filters) | A service forgetting the rule → a deleted notice shows on the home page |
| "Active", "upcoming" and the school/temple counts are computed, never stored | Stored flags and counts going stale |
| One `ModerationStatus` field (Pending → UnderReview → Approved → Published / Rejected) | Impossible combinations of two booleans |
| Every text column is `nvarchar` with an explicit length; enums stored as text | Gujarati turning into `????`; unindexable `nvarchar(max)`; enum reorders changing data meaning |
| `DateTimeOffset` in the database, shown in `Asia/Kolkata` through `IVillageClock` | Servers on UTC making "upcoming" and expiry wrong by 5½ hours |
| Uploads in `App_Data/uploads`, served at `/uploads` (path configurable) | `MapStaticAssets()` only serves build-time files (uploads would 404); redeploys wiping photos |
| Pending resident uploads in `App_Data/private-uploads` | Unreviewed photos reachable by URL |
| Every photo re-encoded (SkiaSharp): JPEG/PNG/WebP only, judged by content; pixel count checked before decoding; turned upright; colours converted to sRGB; saved as WebP in 3 sizes (480/960/1920) under a random name | Multi-MB phone photos on village mobile data; GPS location leaking from EXIF; disguised files; "decompression bombs"; sideways portraits |
| SkiaSharp instead of ImageSharp 4 | ImageSharp 4 needs a licence key to build: Release builds and `dotnet publish` fail without one, and keys expire. ImageSharp 3.1 has had no release since October 2025 while residents' photos (Phase 10) need a maintained decoder |
| Admin at `/admin/…` (attribute routes), English only, every response `no-store`, `noindex`, `X-Frame-Options: DENY` | Admin pages shown again with the Back button on a shared office PC; admin pages in search results; clickjacking |
| Policies `Staff` (Admin or Editor) and `AdminOnly` instead of role strings in attributes | Role names typed differently in different controllers |
| Sign-in: password checked before the "account switched off" message; generic message otherwise; session cookie (not persistent); lockout 5 / 15 min; at most 10 attempts per address per 5 min (429 page explains) | Finding out which emails have accounts; staying signed in on a shared PC; password guessing |
| `DecimalEditFormatMetadataProvider` gives every decimal form box all its digits | ASP.NET Core shows decimals with 2 decimal places, so saving the Village info form unchanged would have moved the map pin up to ~1 km |
| `<bilingual-field for="X" for-gu="XGu" />` tag helper for every two-language field | Each form repeating (and getting wrong) the English/Gujarati pair: `lang="gu"`, limits, hints, validation |
| Known village facts (taluka, district, state) are seed defaults, and a start-up step replaces a value only while it still is the original `[Add …]` placeholder | Facts typed into one database only; or a later seed overwriting what an admin typed |
| The About page's sections are reference data (every environment), not demo data | A live site starting with an empty About page, although its topics come from the spec |
| Upload forms declare `[UploadLimit(images: n, videos: m)]`; the limit is computed from the Storage settings | A two-photo form failing with 413, or a one-photo form accepting 28 MB, whenever MaxImageMegabytes changes |
| `ImageEdit`: upload first, store the path, delete the replaced file only after the save (or the new one if it fails) | Orphaned files, or records pointing at deleted files after a failed save |
| Content screens (About, History, Then & now) use the `Staff` policy; settings screens stay `AdminOnly`; content deletes are soft | Editors unable to write content; one wrong click erasing a page for good |
| A "then & now" pair can be shown only with both photos | A comparison slider with nothing to compare |
| About section addresses become element ids, so `main`, the page's own ids and anything starting with `kc-` are refused | "#…" links and the skip link jumping to the wrong element |
| Timeline, milestone and then-and-now queries live in one place (`Services/History/HistoryQueries.cs`) | Home, About and History showing the same item differently |
| Gujarati pages under `/gu/…`, handled by middleware that moves `/gu` into the PathBase | Google cannot index a cookie-only language. A `{culture}` route parameter was rejected: ASP.NET Core drops it from links to another controller, so links would fall back to English. With PathBase every generated link keeps `/gu` automatically |
| Language comes from the URL only (no cookie, no browser setting) | One URL showing different languages to different visitors and to search engines |
| Route names (`RouteNames`) and "coming soon" placeholder pages for later phases | Broken menu links now, and links to change later: a phase moves the route name to its real page |
| `/places/{slug}` canonical; `/Places/Details/{id}` (the original plan's address) and wrong-case slugs → 301 to it | Split search ranking; old shared links breaking |
| A place is public only when published **and** its category is switched on (`PublicVisibility`) | A place showing a chip and filter link for a category visitors cannot see |
| Pages may declare `ViewData["CanonicalQuery"]` (e.g. `?category=temples&page=2`) and `ViewData["NoIndex"]` (search results) | Category and page 2 pages pointing their canonical at page 1; search result pages indexed |
| One map component (`_MapCanvas` + `map.js`): starts by itself, lazy or eager; one layer per category; pop-up with photo | Three slightly different map set-ups on Home, `/map` and each place's page |
| Place cards, map markers and category counts come from `Services/Places/PlaceQueries.cs` | Home, `/places` and `/map` counting or showing places differently |
| `/map` lists the same places under the map | A map that screen-reader users and visitors without JavaScript cannot use |
| A place's gallery has its own admin page (`/admin/places/{id}/photos`, up to 6 per upload); photo widths and heights are stored | A huge single form; layout jumps while gallery photos load |
| Coordinates rounded half away from zero (`MidpointRounding.AwayFromZero`) | .NET's default banker's rounding (72.9912345 → 72.991234) disagreeing with SQL Server and with people |
| A profile is public only when Published **and** consented **and** its category is switched on (`PublicVisibility`); Approved/Published without consent is also refused by the database | One forgotten condition putting a private villager on the website |
| Consent is a tick **plus** a note of who agreed and how, with the time it was ticked (both admin-only); unticking clears the time | "Did I ever agree?" with no way to answer; a tick nobody can account for |
| Two-person rule (`PersonAdminService`): only Admins approve, show, hide or send back; an Editor's new profile, or a change to what visitors see or to the consent, goes to "Waiting for review" and leaves the website until an Admin approves it. The role comes from the sign-in, never from the form | Personal details published on one person's word; an Editor posting `Status=Published` |
| "Sent back" needs a note; approving, showing and sending back record who decided and when | Editors not knowing what to fix; no answer to "who approved this?" |
| People pages are `noindex` | Consent covers this village website, not a person's name in search engines |
| Removal request = delete the profile (soft): it leaves the website at once; the profile page links to Contact for "change or removal" | A removal waiting for a review cycle |
| Detail pages highlight their section in the menu through `SiteNavigation.SectionFor` (`aria-current="true"`; `"page"` only on the section's own page) | Each detail page setting the highlight by hand, and forgetting to |
| When an event is over is decided once, in `Services/Events/EventTiming.cs` (SQL `NotOver`/`Over` + C# `EndOf`): all-day events last to the end of their last day (`EndsAt` = that day at midnight, or empty for one day); timed events without an end count 3 hours. "Upcoming" and "Past" are exact complements | Home, `/events`, the admin tabs and the "ended" note disagreeing about the same event |
| Event dates and times are entered as village date + clock time and turned into exact moments by `IVillageClock.FromVillageTime`; an end time earlier than the start (without a last day) means the next day | Admins doing time-zone arithmetic; "9 PM – 12:30 AM" Garba nights being refused as "ends before it starts" |
| How a date range reads is in one helper (`ViewModels/Events/EventSchedule.cs`), formatted while the page is written | "17 Oct" vs "17–25 Oct" rules repeated in every view; Gujarati pages showing English month names |
| Event cards come from `Services/Events/EventQueries.cs` (Home, a place's page, `/events`) | The same event reading differently on three pages (this replaced two copies) |
| A new event's web address gets its year (`navratri-garba-night-2026`), on the server and in `admin.js` | Next year's festival colliding with this year's address, or taking over its shared links |
| "Copy as a new event" fills the form from an existing event without dates, web address or photo | Two events sharing one photo file: replacing it on one (ImageEdit deletes the old file) would break the other |
| Home shows 3 upcoming events: featured ones always get a place, then the soonest; shown in date order | The admin having no say, or the Home cards jumping out of date order |
| "Add to calendar" is an `.ics` file written in-house (RFC 5545: CRLF, escaping, 75-byte folding by UTF-8 bytes) | A package for one event with eight fields; Gujarati letters cut in half by naive line folding |
| Share on WhatsApp is a plain `https://wa.me/?text=` link | Third-party share scripts tracking visitors; nothing is sent until the visitor taps it |
| An event's venue is linked, with address and directions, only while that place's page is public | Links to "not found" pages; directions to a place an admin has hidden |
| Shared layout classes `kc-detail-*` (places, events, people) and `MapLinks` for directions/OpenStreetMap links | Copies of the same layout and URL code per page |
| All UI text through `IStringLocalizer` (`@L["…"]`) from Phase 2 | Rewriting every view in Phase 12 |
| Unicode-wide HTML encoder (`WebEncoderOptions`) | The default encoder sent every Gujarati letter as `&#xAAE;`, making Gujarati pages 2–3× larger |
| Icon sprite instead of the Font Awesome icon font | ~320 KB of fonts and CSS for 2,600 icons when the site uses ~70 (sprite: ~15 KB compressed) |
| Fonts with Gujarati + Latin glyphs, split by `unicode-range`, `font-display: swap` | English pages downloading Gujarati font files; invisible text while fonts load |
| Leaflet loaded only when the map nears the screen; markers built with DOM `textContent` | 40 KB of JavaScript for visitors who never scroll to the map; HTML injection through place names |
| Map data in `<script type="application/json">`, no inline JavaScript anywhere | A strict Content-Security-Policy becoming impossible in Phase 13 |
| Public visibility rules in one file (`PublicVisibility.cs`) | One page forgetting a condition and showing unpublished or unconsented content |
| Identity with our own login page, no registration, first admin from User Secrets / env vars | A public `/Register` page; a password in code or git |
| Global antiforgery filter, lockout after 5 failures, rate-limited sign-in (public forms + honeypot from Phase 9) | A forgotten `[ValidateAntiForgeryToken]`; bot spam; password guessing |
| Database CHECK constraints for the critical rules | Invalid data saved by a bug or a manual SQL edit (see §5) |

## 4. Folder structure

```
D:\Dhaval\HarHarMahadev\                      ← git repo root
├─ Khaparwada.slnx
├─ dotnet-tools.json                          pins dotnet-ef 10.0.12
├─ docs\ARCHITECTURE.md                       this file
├─ tools\
│  ├─ icons.txt                               icons in the sprite
│  └─ build-icon-sprite.ps1                   rebuilds wwwroot/images/icons.svg
└─ Khaparwada.Web\
   ├─ Program.cs                              composition root
   ├─ appsettings.json / appsettings.Development.json
   ├─ Areas\Admin\
   │  ├─ Controllers\                         AdminControllerBase (+ admin headers), Account (sign in/out,
   │  │                                       password), Dashboard, VillageInfo, SiteSettings, Categories, UsefulLinks
   │  ├─ Models\                              InputModels, list/view models, AdminNavigation (the sidebar)
   │  └─ Views\                               _AdminLayout, _AuthLayout, _Sidebar, _StatusMessage + one folder per screen
   ├─ Controllers\                            Home, About, History, Places, Map, ComingSoon (placeholders), Error
   ├─ Data\                                   AppDbContext, Configurations, Interceptors, Seed
   ├─ Migrations\
   ├─ Models\                                 Common, Enums, Entities (20)
   ├─ Services\
   │  ├─ About\, History\                     About page; timeline + then & now (HistoryQueries shared with Home)
   │  ├─ Places\                              PlaceService (list, details, map page), PlaceQueries (shared with Home)
   │  ├─ Admin\                               Dashboard, Settings, Category, UsefulLink, AboutSection, Timeline, ThenNow
   │  ├─ Common\                              VillageClock, PublicVisibility, OperationResult
   │  ├─ Home\                                HomeService (one query per Home section)
   │  ├─ Media\                               UploadStorage (folders, random names), ImageUploadService, ImageEdit,
   │  │                                       VideoUploadService, IconCatalog (icons allowed for categories)
   │  ├─ Notices\                             NoticeService (urgent alerts, cached 1 min)
   │  └─ Site\                                SiteInfoService (settings + village info, cached 10 min)
   ├─ ViewModels\                             Shared, Home, About, History, Places, Events, Notices, Gallery, People, Memories, Map
   ├─ ViewComponents\                         PageMeta (head tags), AlertBar, SiteFooter
   ├─ Infrastructure\
   │  ├─ Extensions\                          service registration, middleware registration
   │  ├─ Localization\                        SiteLanguages, LanguagePrefixMiddleware, culture provider, LanguageUrls, AppLanguage
   │  ├─ Media\                               MediaUrls (upload URLs, srcset), UploadLimitAttribute
   │  ├─ ModelBinding\                        DecimalEditFormatMetadataProvider
   │  ├─ Options\                             Site, Map, Storage, Database, Seed, AdminSeed
   │  ├─ Routing\                             RouteNames, SiteNavigation
   │  ├─ Security\                            AppRoles, Policies, AppClaimsPrincipalFactory (display name)
   │  ├─ TagHelpers\                          <icon>, <bilingual-field>
   │  ├─ Text\                                Excerpt, Initials, Paragraphs (Gujarati-safe), SlugGenerator
   │  └─ Validation\                          [HttpUrl]
   ├─ Resources\                              SharedResource (Gujarati .resx in Phase 12)
   ├─ Views\
   │  ├─ Home\                                Index + one partial per section, Privacy, Terms
   │  ├─ About\, History\                     the About page; the timeline + then & now
   │  ├─ Places\, Map\                        place list and page; the village map page
   │  ├─ Shared\                              _Layout, _Header, _PageHeader, _SectionHeading, _ThenNowFigure, ComingSoon,
   │  │                                       Cards\ (place, event, notice, album, person, memory, category),
   │  │                                       Components\ (PageMeta, AlertBar, SiteFooter)
   │  └─ Error\                               NotFound, Status, Error
   ├─ wwwroot\
   │  ├─ css\site.css, admin.css              the design system; the admin panel's additions
   │  ├─ js\                                  site.js (all pages), map.js (pages with a map), admin.js (admin only)
   │  ├─ fonts\                               Rasa, Mukta Vaani (+ OFL licences)
   │  ├─ images\                              icons.svg, brand\ (emblem, share image), motifs\toran.svg,
   │  │                                       illustrations\village-dusk.svg
   │  └─ lib\                                 bootstrap, jquery(+validation), leaflet
   └─ App_Data\                               git-ignored; uploads\ (served at /uploads), private-uploads\ (never served)
```

## 5. Database

**Conventions**

- Every table: `int Id` (identity primary key), `CreatedAt`, `UpdatedAt` — set automatically.
- Content tables: `IsPublished` + soft delete (`IsDeleted`, `DeletedAt`). Lookup tables: `IsActive`.
  Moderated tables: `Status`.
- `X` = English (required), `XGu` = Gujarati (optional; English is shown when it is empty).
- Slugs: unique index filtered on `IsDeleted = 0`, so a deleted page's URL can be reused.
- Images: one `ImagePath` base key; `-sm` / `-md` / `-lg.webp` files follow by naming convention.
  Photo tables also store `Width` / `Height`.
- Map links are generated from latitude / longitude, never stored.
- `VillageInfo` and `SiteSettings` hold exactly one row (`Id = 1`, enforced by a CHECK constraint).
- Admin users are deactivated, never deleted, so "reviewed by" references stay valid.
- EF cascade deletes run during `SaveChanges` (`CascadeTiming.OnSaveChanges`), after the interceptor
  has turned a `Remove()` into a soft delete; otherwise loaded child rows would be hard-deleted.

**Relationships** (`1──<` = one-to-many)

```
PlaceCategory 1──< Place 1──< PlaceImage
                     │
                     └─(optional venue)──< Event
                                             │
                                             └─(optional)──< GalleryAlbum >──1 GalleryCategory
                                                                  │
                                                                  1──< GalleryImage
PersonCategory   1──< Person
MemorySubmission 1──< MemoryImage

ApplicationUser (AspNetUsers) 1──< Person / GalleryImage / MemorySubmission .ReviewedByUserId (optional)
   + Identity tables: AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins,
     AspNetUserTokens, AspNetRoleClaims

No foreign keys: VillageInfo · SiteSettings · AboutSections · HistoryEntries · ThenNowComparisons
                 Notices · UsefulLinks · ContactMessages
```

| Parent → child | On delete | Why |
|---|---|---|
| Category → places / people / albums | Restrict | Deleting a category must never delete its content |
| Place → photos, Album → photos, Memory → photos | Cascade | Photos mean nothing without their parent (permanent deletes only) |
| Place → Event (venue) | Set null | The event keeps its typed location |
| Event → GalleryAlbum | Set null | The album survives without the event link |
| User → ReviewedBy columns | Restrict | Users are deactivated, not deleted |

**Indexes** (foreign-key columns are indexed automatically)

| Table | Index | Used by |
|---|---|---|
| Places | (CategoryId, IsPublished); unique Slug | category filter, map, details page |
| Events | (IsPublished, StartsAt); unique Slug | upcoming / past lists, Home |
| Notices | (IsPublished, PublishedAt); unique Slug | notice board, Home |
| People | (CategoryId, Status); unique Slug | People page |
| GalleryImages | (AlbumId, Status, SortOrder) | album pages |
| HistoryEntries | (IsPublished, SortYear) | timeline |
| MemorySubmissions | (Status, CreatedAt); unique Slug where set | admin review queue |
| ContactMessages | (IsRead, CreatedAt) | admin inbox |

**CHECK constraints** (the database's last line of defence)

| Constraint | Rule |
|---|---|
| `CK_People_ConsentBeforeApproval` | A profile can't be Approved/Published without `HasConsent` |
| `CK_MemorySubmissions_ConsentBeforeApproval` | Same for memories and `ConsentToPublish` |
| `CK_Places_Coordinates`, `CK_VillageInfo_Coordinates` | Latitude and longitude both empty or both valid |
| `CK_Places_WebsiteIsHttp`, `CK_UsefulLinks_UrlIsHttp`, `CK_SiteSettings_HttpLinks` | Links must be http(s) — blocks `javascript:` links |
| `CK_PlaceCategories_MarkerColor` | `#RRGGBB` only (the value is written into styles) |
| `CK_Events_EndsAfterStart`, `CK_Notices_ExpiresAfterPublished` | Date order |
| `CK_VillageInfo_PinCode` | Six digits, not starting with 0 |
| `CK_VillageInfo_SingleRow`, `CK_SiteSettings_SingleRow` | Only `Id = 1` |

## 6. Entities

`Name⁺` means `Name` + `NameGu`. All tables also have `Id`, `CreatedAt`, `UpdatedAt`.

| Entity (table) | Purpose | Main fields | Visibility |
|---|---|---|---|
| ApplicationUser (AspNetUsers) | Admin / Editor accounts | DisplayName, IsActive, LastLoginAt | IsActive |
| VillageInfo (1 row) | Village facts | Name⁺, ShortIntro⁺, Population, Households, AreaSqKm, StatsSource⁺, Taluka⁺, District⁺, State⁺, PinCode, Latitude, Longitude, MapZoom | — |
| AboutSection | About page blocks | Slug, Title⁺, Body⁺, ImagePath, SortOrder | Published · soft delete |
| HistoryEntry | Timeline point | SortYear, YearLabel⁺, Title⁺, Description⁺, ImagePath, IsHighlighted | Published · soft delete |
| ThenNowComparison | Before/after slider | Title⁺, Caption⁺, ThenImagePath, NowImagePath, ThenLabel⁺, NowLabel⁺, SortOrder | Published · soft delete |
| PlaceCategory | Temples, Schools, … | Name⁺, Slug, IconClass, MarkerColor, ShowInStats, SortOrder | IsActive |
| Place | Places + map markers | CategoryId, Name⁺, Slug, Summary⁺, Description⁺, Address⁺, Latitude, Longitude, MainImagePath, Phone, Email, Website, Timings⁺, IsFeatured | Published · soft delete |
| PlaceImage | A place's photos | PlaceId, ImagePath, Width, Height, Caption⁺, SortOrder | via Place |
| PersonCategory | Teachers, Doctors, … | Name⁺, Slug, IconClass, SortOrder | IsActive |
| Person | Community profile (no phone / address / birth date by design) | CategoryId, FullName⁺, Slug, Profession⁺, ShortBio⁺, Achievement⁺, Contribution⁺, PhotoPath, HasConsent, ConsentNote + ConsentRecordedAt (admin-only), IsFeatured, ReviewNote (admin-only), ReviewedBy, ReviewedAt | Status · soft delete |
| Event | Village events | Title⁺, Slug, Summary⁺, Description⁺, StartsAt, EndsAt, IsAllDay, PlaceId, LocationText⁺, ImagePath, IsFeatured | Published · soft delete |
| GalleryCategory | Old Khaparwada, Navratri, … | Name⁺, Slug, SortOrder | IsActive |
| GalleryAlbum | A set of photos | CategoryId, EventId, Title⁺, Slug, Description⁺, TakenOn | Published · soft delete |
| GalleryImage | One photo | AlbumId, ImagePath, Width, Height, Caption⁺, SortOrder, ReviewedBy, ReviewedAt | Status · soft delete |
| Notice | Notice board | Title⁺, Slug, Description⁺, Category, Priority, PublishedAt, ExpiresAt, ImagePath, AttachmentPath, AttachmentName | Published · soft delete |
| MemorySubmission | Resident memory / story | Title, Story, EraLabel, SubmitterName, SubmitterContact (admin-only), ConsentToPublish, ReviewNote (admin-only), ReviewedBy, ReviewedAt, PublishedAt, Slug | Status · soft delete |
| MemoryImage | Photos sent with a memory | MemorySubmissionId, ImagePath, Width, Height, Caption, SortOrder | via memory |
| SiteSetting (1 row) | Website settings | SiteName⁺, Tagline⁺, OfficeEmail, OfficePhone, ShowOfficePhone, OfficeAddress⁺, social URLs, HeroImagePath, HeroVideoPath, DefaultMetaDescription⁺ | — |
| UsefulLink | Contact links + footer "Important Links" | Title⁺, Url, ShowInFooter, SortOrder | IsActive |
| ContactMessage | Contact form inbox (admin-only) | Name, Email, Phone, Subject, Message, IsRead | soft delete |

Enums (stored as text): `ModerationStatus` (Pending, UnderReview, Approved, Published, Rejected),
`NoticeCategory` (General, GovernmentScheme, Water, Electricity, Meeting, Emergency),
`NoticePriority` (Normal, Important, Urgent — active Urgent notices also show a site-wide alert bar).

## 7. Configuration: where each value lives

| Setting | Development | Production |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` (Windows auth, no password) | environment variable `ConnectionStrings__DefaultConnection` |
| `AdminSeed:Email` / `AdminSeed:Password` | User Secrets (right-click project → Manage User Secrets) | environment variables `AdminSeed__Email` / `AdminSeed__Password`; remove after the first start |
| `Database:ApplyMigrationsOnStartup` | `true` | `false` — apply a reviewed script |
| `Database:SqlCompatibilityLevel` | `150` | `150` (raise only if hosting is newer) |
| `Seed:DemoData` | `true` | `false` (and ignored outside Development anyway) |
| `Site:TimeZoneId` | `Asia/Kolkata` | same |
| `Site:PublicBaseUrl` | empty (the request's own address is used) | the real address, e.g. `https://khaparwada.in` — canonical and share links depend on it |
| `Map:TileUrl`, `Map:Attribution`, `Map:MaxZoom` | OpenStreetMap | same, or another tile provider if traffic outgrows OSM's fair-use policy |
| `Storage:UploadsPath`, `Storage:PrivateUploadsPath` | `App_Data/uploads`, `App_Data/private-uploads` (inside the project, git-ignored) | **absolute folders outside the site folder**, e.g. `D:\KhaparwadaData\uploads`, so a redeploy can never delete villagers' photos; back them up with the database |
| `Storage:MaxImageMegabytes` / `MaxImageMegapixels` / `MaxVideoMegabytes` | 10 / 40 / 25 | same. Each upload form's request limit follows from these (`[UploadLimit]`: Site settings = 1 photo + 1 video + 1 MB = 36 MB); on IIS also raise `maxAllowedContentLength` (default ~28.6 MB) in `web.config` |

Do not copy SSMS's connection string into the app: it has no `Database=` (tables would go into `master`),
`Pooling=False` (slow for a website), `Command Timeout=0` (a stuck query hangs forever) and
`Application Name=SQL Server Management Studio`.

## 8. Packages and front-end files

**NuGet**

| Package | Version | Phase | Purpose |
|---|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.12 | 1 | EF Core provider |
| Microsoft.EntityFrameworkCore.Tools | 10.0.12 | 1 | Package Manager Console commands (development only) |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.12 | 1 | Identity tables in `AppDbContext` |
| SkiaSharp | 3.119.4 | 3 | Image pipeline (MIT). Brings the Windows and macOS native library |
| SkiaSharp.NativeAssets.Linux.NoDependencies | 3.119.4 | 3 | The same native library for Linux hosting. Its ~80 MB debug symbols per Windows platform are excluded from bin/publish by a target in the `.csproj` |
| xUnit + Microsoft.AspNetCore.Mvc.Testing | latest | 14 | Test project only |

All Microsoft packages stay on the installed runtime's version; mixed EF versions cause "Method not found"
and migration-tool errors. Built in, no package: localization, output caching, rate limiting, memory cache,
data protection, `TimeProvider`, logging, antiforgery.

**Front-end** (committed under `wwwroot`; each file was checked against the npm package's published SHA-256)

| Library | Version | Source | Files kept |
|---|---|---|---|
| Bootstrap | 5.3.8 | npm `bootstrap` | `bootstrap.min.css`, `bootstrap.bundle.min.js` (+ maps, licence) |
| jQuery + validation | 3.7.1 (template) | template | minified files only; loaded only by `_ValidationScriptsPartial` |
| Leaflet | 1.9.4 | npm `leaflet` | `leaflet.css`, `leaflet.js`, `images/` (+ licence) |
| Font Awesome Free | 7.3.1 | npm `@fortawesome/fontawesome-free` (`svgs-full`) | only the icons in `tools/icons.txt`, in `images/icons.svg` (+ licence) |
| Rasa (variable) | 5.3.0 | npm `@fontsource-variable/rasa` | gujarati, latin, latin-ext `.woff2` (+ OFL) |
| Mukta Vaani 400 / 600 | 5.3.0 | npm `@fontsource/mukta-vaani` | gujarati, latin, latin-ext `.woff2` (+ OFL) |

Deliberately not used: Identity UI package (public registration pages), AutoMapper / MediatR,
generic repository libraries, NetTopologySuite, masonry / lightbox plugins, HtmlSanitizer
(content is plain text), the Font Awesome icon font (see §3).

## 9. Phases

| # | Phase | Status |
|---|---|---|
| 1 | Setup + database + EF Core | **Done** |
| 2 | Layout, navbar, footer, Home (design system, language-aware URLs, placeholders for later sections) | **Done** |
| 3 | Admin foundation: login, roles, dashboard, image upload pipeline, Village Info / Settings / Categories / Useful Links | **Done** |
| 4 | About + History + Then vs Now (public + admin) | **Done** |
| 5 | Places + Map | **Done** |
| 6 | People (approval + consent) | **Done** |
| 7 | Events | **Done** |
| 8 | Gallery (approval, masonry, lightbox) | |
| 9 | Notices + Contact (inbox) | |
| 10 | Memories (submission + review workflow) | |
| 11 | Admin completion: users, pending queue, dashboard polish | |
| 12 | Gujarati / English (resx, hreflang review, Gujarati Privacy & Terms) | |
| 13 | SEO + performance + security hardening | |
| 14 | Testing + deployment | |

Each feature phase ships its public pages and its admin screens together, and replaces its
`ComingSoonController` actions by moving the same route names onto the real pages.

## 10. Developer how-tos

- **Run:** open `Khaparwada.slnx` in Visual Studio, press F5 (https profile). In Development the app
  applies migrations and seeds data on startup.
- **After changing an entity:** Package Manager Console (Default project: `Khaparwada.Web`) →
  `Add-Migration <DescriptiveName>` → review the generated file → F5 (or `Update-Database`).
  CLI equivalent: `dotnet ef migrations add <Name> --project Khaparwada.Web`.
- **Reset the development database:** `dotnet ef database drop --project Khaparwada.Web`
  (or delete it in SSMS), then F5 recreates and reseeds it.
- **Production upgrade script:** `dotnet ef migrations script --idempotent --project Khaparwada.Web -o migrate.sql`,
  review it, run it against the production database.
- **Add an icon:** add a line such as `solid tractor` to `tools/icons.txt` (names:
  https://fontawesome.com/search?ic=free), run `powershell -ExecutionPolicy Bypass -File tools/build-icon-sprite.ps1`
  from the repo root, then use `<icon name="tractor" />` (brands: `<icon brand="whatsapp" />`).
- **Link to a page:** `<a asp-route="@RouteNames.Places">` (never a hard-coded path); on Gujarati pages the
  link gets `/gu` automatically.
- **Reference a static file:** use an absolute path (`/css/site.css`), not `~/` (see §11).
- **Add UI text:** `@L["English text"]` in views; keep the English text as the key.
- **First admin account (development):** right-click the project → *Manage User Secrets* →
  `{ "AdminSeed": { "Email": "you@example.com", "Password": "…12+ characters, A-z, 0-9, symbol…" } }` → F5.
  The startup log says "Created admin account …". Sign in at `/admin`, then change the password
  (sidebar → Password). The secret is only read when the account does not exist yet.
- **Locked out / forgotten password (until Phase 11 adds resets by another admin):** a lockout ends after
  15 minutes. Forgotten password in development: in SSMS run
  `DELETE FROM AspNetUsers WHERE NormalizedEmail = N'YOU@EXAMPLE.COM';`, then F5 re-creates the account from
  User Secrets (possible while nothing yet refers to the account).
- **Add an admin screen (pattern for Phases 5–11; Phase 4's About, Timeline and ThenNow are the model):** controller derived from `AdminControllerBase` with
  `[Route("admin/…")]` (+ `[Authorize(Policy = Policies.AdminOnly)]` if Editors must not use it); entry action
  named `Index`; an InputModel whose ids are `[BindNever]`; a service returning `OperationResult`; redirect after
  saving with `SetStatusMessage`; `<bilingual-field for="X" for-gu="XGu" />` for two-language fields;
  `<partial name="_ValidationScriptsPartial" />` in the Scripts section; a line in `AdminNavigation`. Photo fields:
  `<partial name="_ImageField" model="…" />` (build the model in a code block) and `[UploadLimit(images: n)]` on the
  POST actions; deletes go through the shared `ConfirmDelete` view.
- **Save an uploaded photo:** `var photo = await ImageEdit.PrepareAsync(images, input.Image, input.RemoveImage, "places", ct);`
  → on `photo.Failed` show `photo.Error`; otherwise `entity.ImagePath = photo.Apply(entity.ImagePath)`, save, then
  `photo.SaveSucceeded()` (or `photo.SaveFailed()` when the save throws). Show it with
  `MediaUrls.Image(path, ImageSize.Medium)` plus `srcset="@MediaUrls.SrcSet(path)"`.
- **Show a list with pages:** fill a `PaginationViewModel` (page, size, total, route name, the filter values) and
  `<partial name="_Pagination" model="…" />`; `PaginationViewModel.Clamp` keeps a typed-in page number valid. An admin
  list needs a named route (`[HttpGet("", Name = "admin-places")]`).
- **Show a map:** build a `MapViewModel` and render `<partial name="_MapCanvas" model="new MapCanvasViewModel(…)" />`
  (model built in a code block), then load `/js/map.js` in the Scripts section. Category buttons:
  `data-map-for="<map id>"` with `data-map-filter="<category slug>"` buttons (see `Views/Map/Index.cshtml`).
- **Review a profile:** Admin → People → "Waiting for review" tab (the dashboard links there) → Review → check the
  text and the consent note → choose a decision → Save. "Sent back" needs a note; the Editor sees it on the form.
- **Someone asks for their profile to be removed:** Admin → People → delete. It leaves the website at once; the row
  stays in the database (soft delete) until Phase 11 adds permanent deletion.
- **Test the Editor rules before Phase 11 (no "add user" screen yet):** copy the admin row in `AspNetUsers` with a new
  Id, email and SecurityStamp, and add an `AspNetUserRoles` row for the `Editor` role (the Phase 6 test suite does this
  on its throwaway database). Never do this on the live database.
- **Add next year's festival:** Admin → Events → "Past" tab → the copy button on last year's event → choose the new
  date, check the text, add this year's photo → Add event. The web address gets the new year automatically.
- **Event times:** type them as people in the village say them (IST). For an evening that ends after midnight only
  fill in the end time; the "Last day" is for events over several days (e.g. Navratri, all day or with times).
- **Test with curl from Git Bash:** `export MSYS_NO_PATHCONV=1` first, and send Gujarati values from UTF-8 files
  (`--data-urlencode "NameGu@name.txt"`). See §11.

## 11. Pitfalls found and fixed (read before changing these areas)

| Symptom | Root cause | Rule now |
|---|---|---|
| Renaming the project folder failed with "Permission denied" | A shell's working directory was inside the folder; Windows locks a folder a process is "standing in" | Close VS and move out of a folder before renaming it |
| On Gujarati pages, CSS/fonts would have been downloaded twice | `~/` resolves through PathBase, so it would produce `/gu/css/…` | Static files use absolute `/…` paths; the `<icon>` helper and map pass `PathString.Empty` |
| Gujarati text 2–3× larger than needed in the HTML | Razor's default encoder only allows Basic Latin and writes other letters as `&#x…;` | `WebEncoderOptions` allows all Unicode ranges (`< > & " '` are still encoded) |
| Phones could scroll sideways | (1) `g-5` gutters: rows got −1.5 rem margins inside 0.75 rem container padding. (2) Bootstrap parks the closed slide-out menu off-screen, and that hidden panel widened the page | Use `g-4 g-lg-5`; the closed menu is `display: none` (`@starting-style` keeps the animation). Check with `scrollWidth === clientWidth` at 390 px |
| Phone menu cut off at the header's height | `backdrop-filter` on the header made it the containing block for the `position: fixed` menu inside it | Frosted header only at ≥ 1200 px, where the menu is inline |
| Gujarati conjuncts can break; uppercase means nothing | Letter-spacing and `text-transform` on Gujarati text | Both are switched off under `html[lang="gu"]` |
| Notices sorted Urgent/Important/Normal alphabetically | Enums are stored as text, so `ORDER BY Priority` sorts by the word | Rank explicitly (`OrderByDescending(n => n.Priority == Urgent)…`) |
| A 9 PM event without an end time would vanish at 9:01 PM | "Upcoming" compared only the start time | Events without an end count as running 3 hours (all-day: 24 hours); see `EventTiming` |
| ImageSharp 4 built in Debug with only a warning, but Release builds and `dotnet publish` would fail | ImageSharp 4 checks for a licence key at build time; its target only continues on error in Debug | Image pipeline on SkiaSharp (MIT). Read a package's `build/*.targets` before adopting it |
| Village info showed latitude `21.12` for `21.123457`; saving unchanged would have moved the pin | The input tag helper formats `decimal` as `0.00` when the property has no edit format | `DecimalEditFormatMetadataProvider` gives all decimals their full digits; round on purpose with `[DisplayFormat(ApplyFormatInEditMode = true)]` |
| Admin lists made phones scroll sideways (833 px) | `.visually-hidden` labels are `position: absolute`; the static `.table-responsive` was not their containing block, so they escaped its scroll box | `.table-responsive { position: relative; }` in site.css |
| Menu and close buttons showed on desktop | The rule hiding them came before the rule giving them `display`; with equal specificity the later rule wins | Responsive "hide" rules go after the component's own rules |
| "Account switched off" would tell a stranger which emails have accounts | The account state was checked before the password | `CheckPasswordSignInAsync` first, then the account state, then `SignInAsync` |
| Sign out sat below the fold on 768 px-high screens | The whole sidebar scrolled as one block | Only the menu list scrolls; brand and account stay in view |
| `UPDATE AspNetUsers` from `sqlcmd` failed with Msg 1934 | `sqlcmd` runs with `QUOTED_IDENTIFIER OFF`, and SQL Server refuses to change tables with filtered indexes then | Use SSMS, or `sqlcmd -I` |
| curl tests from Git Bash sent `C:/Program Files/Git/admin/…` and `????` | MSYS rewrites arguments that look like paths and passes arguments to `curl.exe` through the ANSI code page | `export MSYS_NO_PATHCONV=1`; non-English values from UTF-8 files |
| SkiaSharp added ~240 MB to `bin` | Its native package also ships debug symbols (`libSkiaSharp.pdb`, ~80 MB per Windows platform) | Excluded by the `ExcludeSkiaSharpNativeSymbols` target in the `.csproj` |
| `@section.Title` broke the build (RZ2005) | `section` is a Razor keyword (`@section Scripts { }`), so `@section.` starts a directive | Never name a Razor variable `section`, `model`, `inject`, `functions`, `page` or `using` |
| `<partial model="new X(a ? "b" : "c")" />` broke the build | The `"` of a C# string ends the HTML attribute | Build the model in an `@{ }` block and pass the variable |
| MVC1006 in a view helper function | A function that renders tag helpers (`<a asp-route>`, `<icon>`) must be async | `async Task Name() { … }` and `await Name();` |
| A per-form upload limit was ignored | The global antiforgery check (an authorization filter, order 1000) reads the whole form before resource filters run; after that the limit is locked | `UploadLimitAttribute` is an authorization filter with order 900, like ASP.NET Core's `[RequestSizeLimit]` |
| Coordinates came back one millionth off (72.991234 instead of 72.991235) | `Math.Round` uses banker's rounding (half to even) by default | `MidpointRounding.AwayFromZero` for coordinates |
| On phones "Get directions" came after the whole description and gallery | Bootstrap columns can only stack in source order | The place page uses CSS grid areas: photo → card → text on phones |
| About 200 px of empty space under every inner page's header | `.kc-section` padding is meant for stacked Home sections; inner pages added it to the header's own padding | `.kc-page-header + .kc-section` uses a smaller top padding |
| Test scripts copied with PowerShell lost their Gujarati text | Windows PowerShell 5.1 reads BOM-less UTF-8 files as ANSI (and writes a BOM before `#!`) | Copy or edit such files with `sed`, or pass `-Encoding UTF8` |
| A place's or a person's page did not highlight "Places" / "People" in the menu | The header compared the menu item's route name with the page's, and detail pages have their own (`place-details`); the coming-soon pages had hidden it by setting the section by hand | `SiteNavigation.SectionFor` maps detail routes to their section; a new detail route gets a line there |
| `?status=7` would have crashed the admin People list | Enum model binding also accepts numbers, including ones that are not enum values | Check `Enum.IsDefined` on bound enums before using them as keys |
| Names in admin lists squeezed to one word per line on phones | A table shrinks its widest text column first, and the name column has the most text | `.kc-table .kc-name-cell { min-width: 12rem }` for every admin list; the table scrolls sideways instead |
| A test sent "Test Garba, music" for the title "Test Garba, music; dance" | curl's `-F name=value` treats `;` as the start of options such as `type=` | Use `--form-string` for values with `;` |
| `CONVERT(varchar, StartsAt, 127)` showed `…18:30:00Z` for a 00:00 IST date | Style 127 converts a `datetimeoffset` to UTC; the stored value was right (`+05:30`) | Use style 126 to see the stored offset |
| An oversized upload shows "connection reset" instead of the 413 page | The server refuses the body as soon as it passes the limit and closes the connection while the browser is still sending | Expected HTTP/1.1 behaviour. The page checks file sizes before sending (`data-max-bytes`), so people never get there |

## 12. Open items

- Known (October 2026): taluka Gandevi, district Navsari, state Gujarat (seed defaults in `ReferenceDataSeeder`;
  Gujarati ગણદેવી, નવસારી, ગુજરાત to be confirmed). Still needed from the village: Gujarati spelling of
  "Khaparwada", PIN code, village-centre coordinates, real text and photos. Until then the database holds clearly
  marked `[Add …]` / `[Sample]` placeholders.
- Place photos open as plain image pages; the Gallery phase (8) adds a lightbox that the place gallery will reuse.
- The `[Sample]` places sit around a demo point in central Gujarat, not in Gandevi taluka; real places get their
  real locations from the map picker.
- No "bin" screen yet: soft-deleted content is recoverable only in SQL (`IsDeleted = 0`). Planned with the admin
  completion in Phase 11, together with permanently deleting old items and their photo files.
- Native-speaker review of the Gujarati seed names (categories, tagline).
- Profiles published before Phase 6 have consent ticked but no consent note; the form asks for one at their next save.
- Consent is recorded as a note only. If the village prefers signed forms, keep them offline (e.g. at the panchayat
  office) and say so in the note; uploading scans would put signatures on the server and is deliberately not offered.
- Editor accounts can only be created in Phase 11 (Users screen); until then every admin account is an Admin.
- Events have no "cancelled / postponed" state: for now edit the date, or the summary ("Postponed to …"), or hide the
  event. A status (and schema.org `eventStatus`) can be added if the village needs it.
- Photo albums linked to an event (`GalleryAlbum.EventId`) are shown with the Gallery phase (8).
- Privacy policy and Terms are drafts (`Views/Home/Privacy.cshtml`, `Terms.cshtml`) with `[bracketed]` decisions
  to make; consider making them admin-editable pages later. Its statement that photo location data is removed
  is now true: verified in Phase 3 (no EXIF, GPS or camera details in any saved file).
- Hosting (Phase 14): set `Storage:*Path` to folders outside the site folder; on IIS raise
  `maxAllowedContentLength` to 36 MB (the largest upload form: Site settings); Linux hosting is possible (SkiaSharp's Linux library
  is included).
- UI text on `/gu` pages stays English until the Phase 12 translations.
- Hosting choice (affects Phase 14 only). The site must be hosted at the root of its domain (static files use
  absolute paths).
