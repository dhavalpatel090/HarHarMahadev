using System.Globalization;
using System.Text;

namespace Khaparwada.Web.Services.Events;

/// <summary>What the calendar file needs; times in village time.</summary>
public sealed record EventCalendarData(
    int Id, string Title, string Summary, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool IsAllDay,
    string? Location, Uri PageUrl);

/// <summary>
/// "Add to calendar": an iCalendar file (RFC 5545) that phones and computers open in their calendar app. Written
/// here rather than with a package, because it is one event with a handful of fields. The rules that matter:
/// lines end with CRLF; commas, semicolons, backslashes and line breaks in text are escaped; lines longer than
/// 75 bytes are folded (counted in UTF-8 bytes, so Gujarati letters are never cut in half); all-day events use
/// dates, and their end date is the day after the last day.
/// </summary>
public static class EventCalendar
{
    public static string Build(EventCalendarData item, DateTimeOffset now)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Khaparwada Connect//Events//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:event-{item.Id}@{item.PageUrl.Host}",
            $"DTSTAMP:{Utc(now)}"
        };

        if (item.IsAllDay)
        {
            var lastDay = item.EndsAt ?? item.StartsAt;
            lines.Add($"DTSTART;VALUE=DATE:{Date(item.StartsAt)}");
            lines.Add($"DTEND;VALUE=DATE:{Date(lastDay.AddDays(1))}");
        }
        else
        {
            lines.Add($"DTSTART:{Utc(item.StartsAt)}");
            lines.Add($"DTEND:{Utc(EventTiming.EndOf(item.StartsAt, item.EndsAt, isAllDay: false))}");
        }

        lines.Add("SUMMARY:" + Escape(item.Title));
        lines.Add("DESCRIPTION:" + Escape($"{item.Summary}\n\n{item.PageUrl}"));
        if (!string.IsNullOrWhiteSpace(item.Location))
        {
            lines.Add("LOCATION:" + Escape(item.Location));
        }

        lines.Add("URL:" + item.PageUrl);
        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");
        return string.Join("\r\n", lines.SelectMany(Fold)) + "\r\n";
    }

    private static string Utc(DateTimeOffset instant) =>
        instant.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Date(DateTimeOffset villageTime) => villageTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\n")
        .Replace('\r', '\n')
        .Replace("\n", "\\n");

    /// <summary>Lines of at most 75 bytes; each continuation line starts with a space.</summary>
    private static IEnumerable<string> Fold(string line)
    {
        const int limit = 75;
        var builder = new StringBuilder();
        var bytes = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (bytes + rune.Utf8SequenceLength > limit)
            {
                yield return builder.ToString();
                builder.Clear().Append(' ');
                bytes = 1;
            }

            builder.Append(rune.ToString());
            bytes += rune.Utf8SequenceLength;
        }

        yield return builder.ToString();
    }
}
