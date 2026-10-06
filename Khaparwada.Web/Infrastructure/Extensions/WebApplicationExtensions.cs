using Khaparwada.Web.Data.Seed;

namespace Khaparwada.Web.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Upgrades the database (Development only, see Database:ApplyMigrationsOnStartup) and inserts the
    /// data the site needs, before the first request is served.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
    }
}
