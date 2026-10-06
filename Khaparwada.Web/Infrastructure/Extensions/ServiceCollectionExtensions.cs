using System.Text.Encodings.Web;
using System.Text.Unicode;
using Khaparwada.Web.Data;
using Khaparwada.Web.Data.Interceptors;
using Khaparwada.Web.Data.Seed;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Options;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Home;
using Khaparwada.Web.Services.Notices;
using Khaparwada.Web.Services.Site;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;

namespace Khaparwada.Web.Infrastructure.Extensions;

/// <summary>Service registrations grouped by area, so Program.cs reads like a short table of contents.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Binds the typed settings classes and validates them when the app starts, so a wrong setting
    /// fails immediately with a clear message instead of on the first request that needs it.
    /// </summary>
    public static IServiceCollection AddKhaparwadaOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SiteOptions>()
            .Bind(configuration.GetSection(SiteOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                site => TimeZoneInfo.TryFindSystemTimeZoneById(site.TimeZoneId, out _),
                "Site:TimeZoneId must be a time zone id such as \"Asia/Kolkata\".")
            .Validate(
                site => string.IsNullOrWhiteSpace(site.PublicBaseUrl)
                        || (Uri.TryCreate(site.PublicBaseUrl, UriKind.Absolute, out var uri)
                            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)),
                "Site:PublicBaseUrl must be empty or an absolute address such as \"https://khaparwada.in\".")
            .ValidateOnStart();

        services.AddOptions<MapOptions>()
            .Bind(configuration.GetSection(MapOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName));

        services.AddOptions<AdminSeedOptions>()
            .Bind(configuration.GetSection(AdminSeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddKhaparwadaData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. In Development it comes from appsettings.Development.json; " +
                "in production set the environment variable ConnectionStrings__DefaultConnection.");
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(database.SqlCompatibilityLevel));
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IdentitySeeder>();
        services.AddScoped<ReferenceDataSeeder>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    /// <summary>
    /// ASP.NET Core Identity for the admin area. There is deliberately no registration page anywhere:
    /// admins create Editor accounts from the admin panel (Phase 11).
    /// </summary>
    public static IServiceCollection AddKhaparwadaIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>MVC, the two site languages, URL style, caching and the services behind the public pages.</summary>
    public static IServiceCollection AddKhaparwadaWeb(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(SiteLanguages.English);
            options.SupportedCultures = [SiteLanguages.English, SiteLanguages.Gujarati];
            options.SupportedUICultures = [SiteLanguages.English, SiteLanguages.Gujarati];

            // The URL is the only source of the language (see LanguagePrefixCultureProvider).
            options.RequestCultureProviders = [new LanguagePrefixCultureProvider()];
        });

        services.AddControllersWithViews(options =>
            {
                // Check the antiforgery token on every POST/PUT/DELETE, so no form can forget it (CSRF protection).
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            })
            .AddDataAnnotationsLocalization(options =>
                options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

        // Razor's default encoder writes every non-English letter as a numeric code (મ → &#xAAE;), which makes
        // Gujarati pages two to three times larger. Allowing all Unicode ranges sends the text as plain UTF-8;
        // HTML-sensitive characters (< > & " ') are still always encoded, so XSS protection is unchanged.
        services.Configure<WebEncoderOptions>(options =>
            options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

        // "/places" rather than "/Places": one spelling per URL is better for search engines.
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        // One antiforgery cookie for both languages; by default its path would follow the "/gu" PathBase.
        services.AddAntiforgery(options => options.Cookie.Path = "/");

        services.AddMemoryCache();
        services.AddSingleton<IVillageClock, VillageClock>();
        services.AddScoped<ISiteInfoService, SiteInfoService>();
        services.AddScoped<INoticeService, NoticeService>();
        services.AddScoped<IHomeService, HomeService>();

        return services;
    }
}
