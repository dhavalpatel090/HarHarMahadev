using System.Security.Claims;
using Khaparwada.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Infrastructure.Security;

/// <summary>
/// Adds the user's display name to the sign-in cookie, so admin pages can greet the user without a database
/// query on every request.
/// </summary>
public sealed class AppClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    public const string DisplayNameClaim = "display_name";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email ?? "Admin" : user.DisplayName;
        identity.AddClaim(new Claim(DisplayNameClaim, name));
        return identity;
    }
}

public static class ClaimsPrincipalExtensions
{
    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(AppClaimsPrincipalFactory.DisplayNameClaim) ?? user.Identity?.Name ?? "Admin";
}
