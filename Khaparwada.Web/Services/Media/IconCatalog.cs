using System.Text.RegularExpressions;
using Khaparwada.Web.Infrastructure.TagHelpers;

namespace Khaparwada.Web.Services.Media;

/// <summary>The sprite's icons that may stand for a category: offered by the admin icon picker, and checked on save.</summary>
public interface IIconCatalog
{
    /// <summary>Every solid icon except brand logos and interface controls, sorted by name.</summary>
    IReadOnlyList<string> CategoryIcons { get; }

    bool IsCategoryIcon(string? name);
}

public sealed partial class IconCatalog : IIconCatalog
{
    // In the sprite for buttons and navigation only; as a category's icon they would mean nothing.
    private static readonly HashSet<string> InterfaceOnly =
    [
        "bars", "xmark", "arrow-right", "arrow-left", "arrow-up", "chevron-right", "chevron-down", "magnifying-glass",
        "language", "arrow-up-right-from-square", "share-nodes", "quote-left",
        "right-from-bracket", "key", "pen-to-square", "trash-can", "plus", "eye", "lock", "list-check", "inbox", "upload"
    ];

    private readonly HashSet<string> _categoryIcons;

    public IconCatalog(IWebHostEnvironment environment)
    {
        var file = environment.WebRootFileProvider.GetFileInfo(IconTagHelper.SpritePath.TrimStart('/'));
        using var reader = new StreamReader(file.CreateReadStream());

        CategoryIcons = SymbolId().Matches(reader.ReadToEnd())
            .Select(match => match.Groups[1].Value)
            .Where(id => !id.StartsWith("brand-", StringComparison.Ordinal) && !InterfaceOnly.Contains(id))
            .Order(StringComparer.Ordinal)
            .ToList();
        _categoryIcons = CategoryIcons.ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlyList<string> CategoryIcons { get; }

    public bool IsCategoryIcon(string? name) => name is not null && _categoryIcons.Contains(name);

    [GeneratedRegex("<symbol id=\"([^\"]+)\"")]
    private static partial Regex SymbolId();
}
