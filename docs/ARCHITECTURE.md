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
| Images | SixLabors.ImageSharp: validate, rotate, strip metadata, resize, WebP (Phase 3) |
| Tools | Visual Studio 2026, SSMS 22, `dotnet-ef` pinned in `dotnet-tools.json` |

## 2. How a request flows

```
Browser (phone / desktop)
        │ HTTPS
        ▼
Kestrel (IIS in production) → Program.cs middleware, in this order:
  error page / HSTS → friendly status pages (/error/404) → HTTPS redirect
  → language: "/gu/…" becomes PathBase "/gu" + path "/…"  (LanguagePrefixMiddleware)
  → routing → request culture from the URL → authentication → authorization
  → static assets (fingerprinted, pre-compressed) and controllers
  Later phases add: security headers, /uploads, rate limiting, output caching.
        │
        ├── Public controllers       /places/{slug}   /gu/places/{slug}   …
        └── Admin area controllers   /admin/…   [Authorize(Roles = "Admin,Editor")]   (Phase 3)
                 │  thin: call a service → return a view
                 ▼
        Services (one per feature) — public queries all filtered by Services/Common/PublicVisibility.cs
           │                         │                          │
           ▼                         ▼                          ▼
     AppDbContext (EF Core 10)   Media (Phase 3)            IMemoryCache (site settings 10 min,
           │                                                urgent notices 1 min)
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
| Images re-encoded by ImageSharp | Multi-MB phone photos on village mobile data; GPS location leaking from EXIF; disguised files |
| Gujarati pages under `/gu/…`, handled by middleware that moves `/gu` into the PathBase | Google cannot index a cookie-only language. A `{culture}` route parameter was rejected: ASP.NET Core drops it from links to another controller, so links would fall back to English. With PathBase every generated link keeps `/gu` automatically |
| Language comes from the URL only (no cookie, no browser setting) | One URL showing different languages to different visitors and to search engines |
| Route names (`RouteNames`) and "coming soon" placeholder pages for later phases | Broken menu links now, and links to change later: a phase moves the route name to its real page |
| `/places/{slug}` canonical, `/Places/Details/{id}` → 301 (Phase 5) | Split search ranking |
| All UI text through `IStringLocalizer` (`@L["…"]`) from Phase 2 | Rewriting every view in Phase 12 |
| Unicode-wide HTML encoder (`WebEncoderOptions`) | The default encoder sent every Gujarati letter as `&#xAAE;`, making Gujarati pages 2–3× larger |
| Icon sprite instead of the Font Awesome icon font | ~320 KB of fonts and CSS for 2,600 icons when the site uses ~70 (sprite: ~15 KB compressed) |
| Fonts with Gujarati + Latin glyphs, split by `unicode-range`, `font-display: swap` | English pages downloading Gujarati font files; invisible text while fonts load |
| Leaflet loaded only when the map nears the screen; markers built with DOM `textContent` | 40 KB of JavaScript for visitors who never scroll to the map; HTML injection through place names |
| Map data in `<script type="application/json">`, no inline JavaScript anywhere | A strict Content-Security-Policy becoming impossible in Phase 13 |
| Public visibility rules in one file (`PublicVisibility.cs`) | One page forgetting a condition and showing unpublished or unconsented content |
| Identity with our own login page, no registration, first admin from User Secrets / env vars | A public `/Register` page; a password in code or git |
| Global antiforgery filter, lockout after 5 failures, rate-limited public forms + honeypot | A forgotten `[ValidateAntiForgeryToken]`; bot spam; password guessing |
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
   ├─ Controllers\                            Home, ComingSoon (placeholders), Error
   ├─ Data\                                   AppDbContext, Configurations, Interceptors, Seed
   ├─ Migrations\
   ├─ Models\                                 Common, Enums, Entities (20)
   ├─ Services\
   │  ├─ Common\                              VillageClock, PublicVisibility
   │  ├─ Home\                                HomeService (one query per Home section)
   │  ├─ Notices\                             NoticeService (urgent alerts, cached 1 min)
   │  └─ Site\                                SiteInfoService (settings + village info, cached 10 min)
   ├─ ViewModels\                             Shared, Home, Places, Events, Notices, History, Gallery, People, Memories, Map
   ├─ ViewComponents\                         PageMeta (head tags), AlertBar, SiteFooter
   ├─ Infrastructure\
   │  ├─ Extensions\                          service registration, middleware registration
   │  ├─ Localization\                        SiteLanguages, LanguagePrefixMiddleware, culture provider, LanguageUrls, AppLanguage
   │  ├─ Media\                               MediaUrls (upload URLs, default artwork)
   │  ├─ Options\                             Site, Map, Database, Seed, AdminSeed
   │  ├─ Routing\                             RouteNames, SiteNavigation
   │  ├─ Security\                            AppRoles
   │  ├─ TagHelpers\                          <icon> tag helper
   │  └─ Text\                                Excerpt, Initials, Paragraphs (Gujarati-safe)
   ├─ Resources\                              SharedResource (Gujarati .resx in Phase 12)
   ├─ Views\
   │  ├─ Home\                                Index + one partial per section, Privacy, Terms
   │  ├─ Shared\                              _Layout, _Header, _PageHeader, _SectionHeading, ComingSoon,
   │  │                                       Cards\ (place, event, notice, album, person, memory, category),
   │  │                                       Components\ (PageMeta, AlertBar, SiteFooter)
   │  └─ Error\                               NotFound, Status, Error
   ├─ wwwroot\
   │  ├─ css\site.css                         the design system
   │  ├─ js\                                  site.js (all pages), home.js, map.js
   │  ├─ fonts\                               Rasa, Mukta Vaani (+ OFL licences)
   │  ├─ images\                              icons.svg, brand\ (emblem, share image), motifs\toran.svg,
   │  │                                       illustrations\village-dusk.svg
   │  └─ lib\                                 bootstrap, jquery(+validation), leaflet
   └─ App_Data\                               git-ignored; uploads (Phase 3)
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
| Person | Community profile (no phone / address / birth date by design) | CategoryId, FullName⁺, Slug, Profession⁺, ShortBio⁺, Achievement⁺, Contribution⁺, PhotoPath, HasConsent, IsFeatured, ReviewedBy, ReviewedAt | Status · soft delete |
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
| SixLabors.ImageSharp | 4.1.x | 3 | Image pipeline (Six Labors Split License: Apache 2.0 for individuals / non-profits under US$1M revenue) |
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
| 3 | Admin foundation: login, roles, dashboard, image upload pipeline, Village Info / Settings / Categories / Useful Links | |
| 4 | About + History + Then vs Now (public + admin) | |
| 5 | Places + Map | |
| 6 | People (approval + consent) | |
| 7 | Events | |
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
| A 9 PM event without an end time would vanish at 9:01 PM | "Upcoming" compared only the start time | Events without an end count as running 3 hours (all-day: 24 hours) |

## 12. Open items

- From the village: Gujarati spelling of "Khaparwada", taluka, district, state, village-centre coordinates,
  real text and photos. Until then the database holds clearly marked `[Add …]` / `[Sample]` placeholders.
- Native-speaker review of the Gujarati seed names (categories, tagline).
- Privacy policy and Terms are drafts (`Views/Home/Privacy.cshtml`, `Terms.cshtml`) with `[bracketed]` decisions
  to make; consider making them admin-editable pages later. The privacy policy's statement that photo
  location data is removed depends on the Phase 3 image pipeline.
- UI text on `/gu` pages stays English until the Phase 12 translations.
- Hosting choice (affects Phase 14 only). The site must be hosted at the root of its domain (static files use
  absolute paths).
