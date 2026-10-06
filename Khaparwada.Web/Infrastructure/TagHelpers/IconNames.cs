using System.Text.RegularExpressions;

namespace Khaparwada.Web.Infrastructure.TagHelpers;

/// <summary>Maps icon names to symbol ids in the SVG sprite (wwwroot/images/icons.svg).</summary>
public static partial class IconNames
{
    private static readonly HashSet<string> Modifiers =
    [
        "fa-solid", "fa-regular", "fa-brands", "fa-fw", "fa-xs", "fa-sm", "fa-lg", "fa-xl", "fa-2x", "fa-3x"
    ];

    /// <summary>
    /// "gopuram" → "gopuram"; "fa-solid fa-gopuram" (as stored for categories) → "gopuram";
    /// "fa-brands fa-whatsapp" → "brand-whatsapp".
    /// </summary>
    public static string ToSymbol(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return "circle-info";
        }

        var tokens = icon.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var name = tokens.LastOrDefault(t => t.StartsWith("fa-", StringComparison.Ordinal) && !Modifiers.Contains(t));
        if (name is null)
        {
            return tokens[^1];
        }

        name = name["fa-".Length..];
        return tokens.Contains("fa-brands") ? "brand-" + name : name;
    }

    /// <summary>True for "#RRGGBB" (the only colour format the database accepts).</summary>
    public static bool IsHexColor(string? value) => value is not null && HexColorPattern().IsMatch(value);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorPattern();
}
