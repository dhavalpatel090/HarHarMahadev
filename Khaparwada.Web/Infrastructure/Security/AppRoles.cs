namespace Khaparwada.Web.Infrastructure.Security;

/// <summary>Role names for [Authorize(Roles = ...)]. Constants, so a typo is a compile error, not a security hole.</summary>
public static class AppRoles
{
    /// <summary>Full access, including users and site settings.</summary>
    public const string Admin = "Admin";

    /// <summary>Can add and edit content; their uploads wait for an Admin's approval.</summary>
    public const string Editor = "Editor";

    public static readonly IReadOnlyList<string> All = [Admin, Editor];
}
