using Khaparwada.Web.Data;
using Khaparwada.Web.Data.Interceptors;
using Khaparwada.Web.Data.Seed;
using Khaparwada.Web.Infrastructure.Options;
using Khaparwada.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
}
