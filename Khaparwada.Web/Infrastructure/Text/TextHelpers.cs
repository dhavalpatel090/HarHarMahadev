using System.Globalization;
using System.Text.RegularExpressions;

namespace Khaparwada.Web.Infrastructure.Text;

public static partial class TextHelpers
{
    /// <summary>
    /// Shortens text for cards and previews. It cuts at a space, so it never splits a word; when it has to
    /// cut inside a very long word, it cuts between whole letters (text elements), so a Gujarati consonant
    /// never loses its vowel sign.
    /// </summary>
    public static string Excerpt(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var clean = Whitespace().Replace(text, " ").Trim();
        if (clean.Length <= maxLength)
        {
            return clean;
        }

        var cut = clean.LastIndexOf(' ', maxLength);
        if (cut < maxLength / 2)
        {
            cut = StringInfo.ParseCombiningCharacters(clean).LastOrDefault(index => index <= maxLength);
        }

        return clean[..cut].TrimEnd(' ', ',', '.', ';', ':', '-', '–') + "…";
    }

    /// <summary>
    /// Up to two initials for an avatar, e.g. "Teacher Profile" → "TP". Each initial is a whole letter
    /// (text element), so Gujarati initials keep their vowel signs.
    /// </summary>
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var initials = new List<string>(capacity: 2);
        foreach (var word in name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var letters = StringInfo.GetTextElementEnumerator(word);
            while (letters.MoveNext())
            {
                var letter = letters.GetTextElement();
                if (char.IsLetter(letter[0]))
                {
                    initials.Add(letter.ToUpperInvariant());
                    break;
                }
            }

            if (initials.Count == 2)
            {
                break;
            }
        }

        return initials.Count == 0 ? "?" : string.Concat(initials);
    }

    /// <summary>
    /// Splits plain text into paragraphs at blank lines. Content is stored as plain text (never HTML), so
    /// views render each paragraph HTML-encoded and nothing typed in Admin can inject markup or script.
    /// </summary>
    public static IEnumerable<string> Paragraphs(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : BlankLines().Split(text.Trim()).Select(paragraph => paragraph.Trim()).Where(paragraph => paragraph.Length > 0);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"\r?\n\s*\r?\n")]
    private static partial Regex BlankLines();
}
