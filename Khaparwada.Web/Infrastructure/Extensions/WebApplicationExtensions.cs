using Khaparwada.Web.Data.Seed;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Services.Media;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

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

    /// <summary>"/gu/…" serves the Gujarati site (see <see cref="LanguagePrefixMiddleware"/>). Must run before routing.</summary>
    public static IApplicationBuilder UseLanguageFromUrl(this IApplicationBuilder app) =>
        app.UseMiddleware<LanguagePrefixMiddleware>();

    /// <summary>
    /// Serves uploaded files at /uploads from the uploads folder, which is outside wwwroot: MapStaticAssets only
    /// knows the files that existed at build time, and a deployment must never be able to delete uploads.
    /// </summary>
    public static IApplicationBuilder UseUploadedFiles(this WebApplication app)
    {
        var storage = app.Services.GetRequiredService<IUploadStorage>();

        // Only these types are ever served from the uploads folder; any other file answers 404.
        var contentTypes = new FileExtensionContentTypeProvider(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".webp"] = "image/webp",
            [".mp4"] = "video/mp4",
            [".pdf"] = "application/pdf"
        });

        return app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = MediaUrls.RequestPath,
            FileProvider = new PhysicalFileProvider(storage.PublicRoot),
            ContentTypeProvider = contentTypes,
            OnPrepareResponse = context =>
            {
                var headers = context.Context.Response.Headers;
                // Every upload gets a new random name, so a URL's content never changes: cache it for a year.
                headers.CacheControl = "public, max-age=31536000, immutable";
                headers.XContentTypeOptions = "nosniff";
            }
        });
    }
}
