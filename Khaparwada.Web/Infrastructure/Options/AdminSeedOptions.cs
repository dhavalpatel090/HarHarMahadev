using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Infrastructure.Options;

/// <summary>
/// The first admin account, created at startup if it doesn't exist yet. Never put these values in
/// appsettings.json: use User Secrets in Development and environment variables in production
/// (AdminSeed__Email, AdminSeed__Password).
/// </summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    [EmailAddress]
    public string? Email { get; set; }

    public string? Password { get; set; }

    [StringLength(FieldLengths.Name)]
    public string DisplayName { get; set; } = "Administrator";
}
