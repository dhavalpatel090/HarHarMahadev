using Khaparwada.Web.Infrastructure.Options;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Data.Seed;

/// <summary>
/// Creates the Admin and Editor roles and, on first run, the first admin account from configuration
/// (User Secrets in Development, environment variables in production). There is no password in code.
/// </summary>
public sealed class IdentitySeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminSeedOptions> adminOptions,
    ILogger<IdentitySeeder> logger)
{
    public async Task SeedAsync()
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                ThrowIfFailed(await roleManager.CreateAsync(new IdentityRole(role)), $"create the '{role}' role");
            }
        }

        var admin = adminOptions.Value;
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogWarning(
                "No admin account was created because AdminSeed:Email and AdminSeed:Password are not set. " +
                "In Visual Studio: right-click the Khaparwada.Web project > Manage User Secrets.");
            return;
        }

        // Only ever creates the account. An existing admin's password is never reset from configuration,
        // otherwise every restart would undo a password change.
        if (await userManager.FindByEmailAsync(admin.Email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = admin.Email,
            Email = admin.Email,
            EmailConfirmed = true,
            DisplayName = admin.DisplayName,
            IsActive = true
        };

        ThrowIfFailed(await userManager.CreateAsync(user, admin.Password), "create the admin account");
        ThrowIfFailed(await userManager.AddToRoleAsync(user, AppRoles.Admin), "add the admin account to the Admin role");
        logger.LogInformation("Created admin account {Email}.", admin.Email);
    }

    private static void ThrowIfFailed(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            var reasons = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not {action}: {reasons}");
        }
    }
}
