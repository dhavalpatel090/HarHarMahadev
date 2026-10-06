namespace Khaparwada.Web.Infrastructure.Security;

/// <summary>Names of the authorization and rate-limiting policies (registered in ServiceCollectionExtensions).</summary>
public static class Policies
{
    /// <summary>Admins and Editors: everyone who may sign in to /admin.</summary>
    public const string Staff = "Staff";

    /// <summary>Admins only: users, settings and the structure of the site.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Sign-in attempts per network address. Lockout protects one account; this slows down someone trying
    /// many passwords against many accounts.
    /// </summary>
    public const string LoginRateLimit = "login";
}
