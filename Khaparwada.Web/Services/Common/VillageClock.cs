using Khaparwada.Web.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Services.Common;

/// <summary>
/// "Now" and the village's time zone in one place. The database stores exact instants; this shows them in
/// Indian time even on a server that runs on UTC. Tests can replace TimeProvider to fix "now".
/// </summary>
public interface IVillageClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>The same instant, expressed in the village's time zone (IST).</summary>
    DateTimeOffset ToVillageTime(DateTimeOffset instant);
}

public sealed class VillageClock(TimeProvider timeProvider, IOptions<SiteOptions> options) : IVillageClock
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateTimeOffset ToVillageTime(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, _timeZone);
}
