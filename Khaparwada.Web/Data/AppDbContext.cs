using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Models.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Khaparwada.Web.Data;

/// <summary>
/// The application's single database context: the ASP.NET Core Identity tables plus all village content.
/// Table details (lengths, indexes, relationships, filters) live in Data/Configurations, one class per entity.
/// With nullable reference types on, a <c>string</c> property becomes a NOT NULL column and a
/// <c>string?</c> property a NULL column.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        // Apply cascade deletes during SaveChanges, after AuditSaveChangesInterceptor has turned a
        // Remove() of a soft-deletable row into an update. With the default (immediate) timing,
        // removing a Place would at once mark its loaded photos as Deleted, and they would be
        // permanently deleted even though the Place itself is only hidden.
        ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
        ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
    }

    public DbSet<VillageInfo> VillageInfo => Set<VillageInfo>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<AboutSection> AboutSections => Set<AboutSection>();
    public DbSet<HistoryEntry> HistoryEntries => Set<HistoryEntry>();
    public DbSet<ThenNowComparison> ThenNowComparisons => Set<ThenNowComparison>();
    public DbSet<PlaceCategory> PlaceCategories => Set<PlaceCategory>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<PlaceImage> PlaceImages => Set<PlaceImage>();
    public DbSet<PersonCategory> PersonCategories => Set<PersonCategory>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<GalleryCategory> GalleryCategories => Set<GalleryCategory>();
    public DbSet<GalleryAlbum> GalleryAlbums => Set<GalleryAlbum>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<MemorySubmission> MemorySubmissions => Set<MemorySubmission>();
    public DbSet<MemoryImage> MemoryImages => Set<MemoryImage>();
    public DbSet<UsefulLink> UsefulLinks => Set<UsefulLink>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity tables; must run first.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        EnsureSoftDeleteFilters(builder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Enums are stored as readable text ("Published", not 3): easy to read in SSMS, and
        // reordering an enum's members can never change what existing rows mean.
        configurationBuilder.Properties<ModerationStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<NoticeCategory>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<NoticePriority>().HaveConversion<string>().HaveMaxLength(20);
    }

    /// <summary>
    /// Fails fast if a soft-deletable entity has no query filter; without one, deleted rows would
    /// quietly appear on the public site.
    /// </summary>
    private static void EnsureSoftDeleteFilters(ModelBuilder builder)
    {
        var missing = builder.Model.GetEntityTypes()
            .Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType) && t.GetDeclaredQueryFilters().Count == 0)
            .Select(t => t.ClrType.Name)
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Soft-deletable entities without a query filter: {string.Join(", ", missing)}. " +
                "Add builder.HasQueryFilter(x => !x.IsDeleted) to their configuration class.");
        }
    }
}
