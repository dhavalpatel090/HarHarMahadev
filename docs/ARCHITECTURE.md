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
| UI | Razor views, Bootstrap 5, small plain-JS files; jQuery only for form validation |
| Map | Leaflet + OpenStreetMap tiles, tile URL in configuration (Phase 5) |
| Images | SixLabors.ImageSharp: validate, rotate, strip metadata, resize, WebP (Phase 3) |
| Tools | Visual Studio 2026, SSMS 22, `dotnet-ef` pinned in `dotnet-tools.json` |

## 2. How a request flows

```
Browser (phone / desktop)
        │ HTTPS
        ▼
Kestrel (IIS in production) → Program.cs middleware, in this order:
  error page/HSTS → HTTPS redirect → security headers
  → static assets (css/js, fingerprinted) → /uploads (runtime images)
  → routing → language (en | gu) → rate limiter → authentication → authorization → output cache
        │
        ├── Public controllers       /places/{slug}   /gu/places/{slug}   …
        └── Admin area controllers   /admin/…   [Authorize(Roles = "Admin,Editor")]
                 │  thin: validate → call service → return view / redirect
                 ▼
        Services (one per feature: business rules, queries, moderation, caching)
           │                         │                          │
           ▼                         ▼                          ▼
     AppDbContext (EF Core 10)   Media (ImageSharp,         IMemoryCache /
           │                     file storage)              output-cache tags
           ▼                         ▼
   SQL Server: KhaparwadaConnect   App_Data/uploads          (public, approved)
                                   App_Data/private-uploads  (pending, admin-only)
```

Views receive ViewModels only; forms bind to InputModels. Entities never reach a view or a form.
Items in the pipeline above that belong to later phases (security headers, language, rate limiter,
output cache, /uploads) are added in those phases.

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
| `DateTimeOffset` in the database, shown in `Asia/Kolkata`, time read via `TimeProvider` | Servers on UTC making "upcoming" and expiry wrong by 5½ hours |
| Uploads in `App_Data/uploads`, served at `/uploads` (path configurable) | `MapStaticAssets()` only serves build-time files (uploads would 404); redeploys wiping photos |
| Pending resident uploads in `App_Data/private-uploads` | Unreviewed photos reachable by URL |
| Images re-encoded by ImageSharp | Multi-MB phone photos on village mobile data; GPS location leaking from EXIF; disguised files |
| `/places/{slug}` canonical, `/Places/Details/{id}` → 301; Gujarati under `/gu/` | Split search ranking; Gujarati pages invisible to Google with a cookie-only language switch |
| All UI text through `IStringLocalizer` from Phase 2 | Rewriting every view in Phase 12 |
| Leaflet + OpenStreetMap, tile URL in config | Google Maps needs an API key with billing |
| Identity with our own login page, no registration, first admin from User Secrets / env vars | A public `/Register` page; a password in code or git |
| Global antiforgery filter, lockout after 5 failures, rate-limited public forms + honeypot | A forgotten `[ValidateAntiForgeryToken]`; bot spam; password guessing |
| No inline JavaScript from Phase 2 | A strict Content-Security-Policy becoming impossible in Phase 13 |
| Database CHECK constraints for the critical rules | Invalid data saved by a bug or a manual SQL edit (see §5) |

## 4. Folder structure

✓ = exists after Phase 1.

```
D:\Dhaval\HarHarMahadev\                      ← git repo root
├─ Khaparwada.slnx                       ✓
├─ dotnet-tools.json                     ✓  pins dotnet-ef 10.0.12 (.NET 10 default location)
├─ docs\ARCHITECTURE.md                  ✓  this file
├─ Khaparwada.Web\
│  ├─ Program.cs                         ✓  short composition root
│  ├─ appsettings.json                   ✓  non-secret defaults only
│  ├─ appsettings.Development.json       ✓  local SQLEXPRESS connection (no password)
│  ├─ Areas\Admin\ Controllers\ Models\ Views\          (Phase 3)
│  ├─ Controllers\                       ✓  HomeController (template; reworked in Phase 2)
│  ├─ Models\
│  │  ├─ Common\                         ✓  BaseEntity, ISoftDeletable, FieldLengths
│  │  ├─ Enums\                          ✓  ModerationStatus, NoticeCategory, NoticePriority
│  │  └─ Entities\                       ✓  20 entity classes
│  ├─ ViewModels\                        ✓  ErrorViewModel (more per feature)
│  ├─ Data\
│  │  ├─ AppDbContext.cs                 ✓
│  │  ├─ Configurations\                 ✓  one IEntityTypeConfiguration<T> per entity + SqlConstraints
│  │  ├─ Interceptors\                   ✓  AuditSaveChangesInterceptor
│  │  └─ Seed\                           ✓  DatabaseInitializer, IdentitySeeder, ReferenceDataSeeder, DemoDataSeeder
│  ├─ Migrations\                        ✓  generated by EF
│  ├─ Services\<Feature>\                   interface + implementation side by side (from Phase 2)
│  ├─ Infrastructure\
│  │  ├─ Options\                        ✓  SiteOptions, DatabaseOptions, SeedOptions, AdminSeedOptions
│  │  ├─ Extensions\                     ✓  service registration, startup database initialisation
│  │  ├─ Localization\                      (Phase 2 / 12)
│  │  └─ Security\                       ✓  AppRoles (headers middleware, upload validation later)
│  ├─ ViewComponents\                       (Phase 2)
│  ├─ Resources\                            SharedResource.gu.resx (Phase 12)
│  ├─ Views\                             ✓  template views (reworked from Phase 2)
│  ├─ wwwroot\ css\ js\ images\ fonts\ lib\
│  └─ App_Data\                             git-ignored; uploads (Phase 3)
└─ Khaparwada.Web.Tests\                    Phase 14
```

There is deliberately no `Interfaces\` or `Repositories\` folder: each interface sits next to its
implementation, and `DbContext` replaces repositories (see §3).

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

Storage and Map settings are added in Phases 3 and 5, when code first reads them.

Do not copy SSMS's connection string into the app: it has no `Database=` (tables would go into `master`),
`Pooling=False` (slow for a website), `Command Timeout=0` (a stuck query hangs forever) and
`Application Name=SQL Server Management Studio`.

## 8. Packages

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

Front-end (files under `wwwroot/lib`): Bootstrap 5.3.8, jQuery 3.7.1 + validation (template),
Leaflet 1.9.4, Font Awesome Free 7.x, self-hosted fonts with both Gujarati and Latin glyphs.

Deliberately not used: Identity UI package (public registration pages), AutoMapper / MediatR,
generic repository libraries, NetTopologySuite, masonry / lightbox plugins, HtmlSanitizer
(content is plain text).

## 9. Phases

| # | Phase | Status |
|---|---|---|
| 1 | Setup + database + EF Core | **Done** |
| 2 | Layout, navbar, footer, Home (design system, language-aware routes) | |
| 3 | Admin foundation: login, roles, dashboard, image upload pipeline, Village Info / Settings / Categories / Useful Links | |
| 4 | About + History + Then vs Now (public + admin) | |
| 5 | Places + Map | |
| 6 | People (approval + consent) | |
| 7 | Events | |
| 8 | Gallery (approval, masonry, lightbox) | |
| 9 | Notices + Contact (inbox) | |
| 10 | Memories (submission + review workflow) | |
| 11 | Admin completion: users, pending queue, dashboard polish | |
| 12 | Gujarati / English (resx, switcher, hreflang) | |
| 13 | SEO + performance + security hardening | |
| 14 | Testing + deployment | |

Each feature phase ships its public pages and its admin screens together.

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

## 11. Open items

- From the village: Gujarati spelling of "Khaparwada", taluka, district, state, village-centre coordinates,
  real text and photos. Until then the database holds clearly marked `[Add …]` / `[Sample]` placeholders.
- Native-speaker review of the Gujarati seed names (categories, tagline).
- Hosting choice (affects Phase 14 only).
