using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Infrastructure.Text;

/// <summary>
/// Web addresses ("slugs") such as "hospitals-health-centers". Only a–z, 0–9 and hyphens: slugs are always
/// made from the English name, because Gujarati letters in a URL turn into long "%E0%AA…" codes when a
/// link is shared on WhatsApp.
/// </summary>
public static partial class SlugGenerator
{
    /// <summary>"Hospitals &amp; Health Centers" → "hospitals-health-centers". Returns "" if no usable letters remain.</summary>
    public static string Create(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue; // é → e
            }

            var lower = char.ToLowerInvariant(character);
            builder.Append(lower is >= 'a' and <= 'z' or >= '0' and <= '9' ? lower : '-');
        }

        var slug = RepeatedHyphens().Replace(builder.ToString(), "-").Trim('-');
        return slug.Length <= FieldLengths.Slug ? slug : slug[..FieldLengths.Slug].TrimEnd('-');
    }

    public static bool IsValid(string? slug) =>
        slug is { Length: > 0 and <= FieldLengths.Slug } && ValidSlug().IsMatch(slug);

    /// <summary>Pattern for form validation attributes.</summary>
    public const string Pattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedHyphens();

    [GeneratedRegex(Pattern)]
    private static partial Regex ValidSlug();
}
