using Khaparwada.Web.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Data.Seed;

/// <summary>
/// Runs once at startup: migrations (if enabled) → roles and first admin → required reference data →
/// demo content (Development only). Every step only inserts what is missing, so it is safe on every start
/// and never overwrites what an admin has edited.
/// </summary>
public sealed class DatabaseInitializer(
    AppDbContext db,
    IdentitySeeder identitySeeder,
    ReferenceDataSeeder referenceDataSeeder,
    DemoDataSeeder demoDataSeeder,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<SeedOptions> seedOptions,
    IHostEnvironment environment,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (databaseOptions.Value.ApplyMigrationsOnStartup)
        {
            logger.LogInformation("Applying pending database migrations (Database:ApplyMigrationsOnStartup = true).");
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count > 0)
            {
                throw new InvalidOperationException(
                    $"The database is missing {pending.Count} migration(s): {string.Join(", ", pending)}. " +
                    "Apply them first: Update-Database in Package Manager Console, or the reviewed SQL script in production.");
            }
        }

        await identitySeeder.SeedAsync();
        await referenceDataSeeder.SeedAsync(cancellationToken);

        if (!seedOptions.Value.DemoData)
        {
            return;
        }

        if (environment.IsDevelopment())
        {
            await demoDataSeeder.SeedAsync(cancellationToken);
        }
        else
        {
            logger.LogWarning("Seed:DemoData is ignored outside Development, so sample content never reaches a live site.");
        }
    }
}
