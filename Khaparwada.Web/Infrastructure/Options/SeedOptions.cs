namespace Khaparwada.Web.Infrastructure.Options;

/// <summary>Settings from the "Seed" section of appsettings.json.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Insert [Sample] content into an empty database. Ignored outside Development.</summary>
    public bool DemoData { get; set; }
}
