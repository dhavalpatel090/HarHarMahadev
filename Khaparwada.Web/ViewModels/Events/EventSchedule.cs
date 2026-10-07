using System.Globalization;

namespace Khaparwada.Web.ViewModels.Events;

/// <summary>
/// How an event's date and time read, from times already in village time. One place for the cases:
/// <list type="bullet">
/// <item>one day: "Sat, 17 Oct 2026" and "9:00 PM – 11:00 PM";</item>
/// <item>an evening that ends after midnight still reads as one date: "9:00 PM – 12:30 AM";</item>
/// <item>several days: "17 – 25 Oct 2026", "28 Oct – 2 Nov 2026", "28 Dec 2026 – 2 Jan 2027";</item>
/// <item>all day: no time (the view writes "All day" in the page's language).</item>
/// </list>
/// The culture is passed in, so Gujarati pages get Gujarati month and day names.
/// </summary>
public static class EventSchedule
{
    /// <summary>All-day events on more than one date; timed events lasting a day or more.</summary>
    public static bool IsMultiDay(DateTimeOffset start, DateTimeOffset? end, bool isAllDay) =>
        end is { } last && (isAllDay ? last.Date > start.Date : last - start >= TimeSpan.FromDays(1));

    public static string DateText(DateTimeOffset start, DateTimeOffset? end, bool isAllDay, CultureInfo culture)
    {
        if (!IsMultiDay(start, end, isAllDay))
        {
            return start.ToString("ddd, d MMM yyyy", culture);
        }

        var last = end!.Value;
        var lastText = last.ToString("d MMM yyyy", culture);
        if (start.Year != last.Year)
        {
            return $"{start.ToString("d MMM yyyy", culture)} – {lastText}";
        }

        return start.Month != last.Month
            ? $"{start.ToString("d MMM", culture)} – {lastText}"
            : $"{start.ToString("%d", culture)} – {lastText}";
    }

    /// <summary>"9:00 PM" or "9:00 PM – 12:30 AM"; null for all-day events.</summary>
    public static string? TimeText(DateTimeOffset start, DateTimeOffset? end, bool isAllDay, CultureInfo culture)
    {
        if (isAllDay)
        {
            return null;
        }

        var from = start.ToString("h:mm tt", culture);
        return end is { } last ? $"{from} – {last.ToString("h:mm tt", culture)}" : from;
    }
}
