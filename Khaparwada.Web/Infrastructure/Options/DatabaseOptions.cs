using System.ComponentModel.DataAnnotations;

namespace Khaparwada.Web.Infrastructure.Options;

/// <summary>Settings from the "Database" section of appsettings.json.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Apply pending migrations when the app starts. True in Development only: a production database is
    /// upgraded deliberately with a reviewed script, never as a side effect of a restart.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>
    /// SQL Server version EF Core writes SQL for. 150 = SQL Server 2019, so the generated SQL also runs on
    /// typical hosting even though development uses SQL Server 2025 (170).
    /// </summary>
    [Range(130, 170)]
    public int SqlCompatibilityLevel { get; set; } = 150;
}
