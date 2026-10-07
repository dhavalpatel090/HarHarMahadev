using System.Globalization;
using Khaparwada.Web.Infrastructure.Media;

namespace Khaparwada.Web.ViewModels.Events;

public sealed class EventCardViewModel
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }

    /// <summary>Start, already converted to the village's time zone.</summary>
    public required DateTimeOffset StartsAt { get; init; }

    /// <summary>End, already converted to the village's time zone.</summary>
    public DateTimeOffset? EndsAt { get; init; }
    public bool IsAllDay { get; init; }
    public string? Location { get; init; }
    public string? ImagePath { get; init; }

    public string? ImageUrl => MediaUrls.Image(ImagePath);

    // Read while the page is written, so the request's language (English or Gujarati) is used.
    public string DateText => EventSchedule.DateText(StartsAt, EndsAt, IsAllDay, CultureInfo.CurrentCulture);
    public string? TimeText => EventSchedule.TimeText(StartsAt, EndsAt, IsAllDay, CultureInfo.CurrentCulture);
}
