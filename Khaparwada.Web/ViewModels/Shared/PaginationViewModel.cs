namespace Khaparwada.Web.ViewModels.Shared;

/// <summary>
/// "Page 2 of 5" links for any list (Views/Shared/_Pagination). Links are made from the route name and the
/// list's other query values (category, search), so a filter is kept while paging.
/// </summary>
public sealed class PaginationViewModel
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
    public required string RouteName { get; init; }
    public IReadOnlyDictionary<string, string?> RouteValues { get; init; } = new Dictionary<string, string?>();

    public int TotalPages => Math.Max(1, (TotalItems + PageSize - 1) / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    /// <summary>The page from the URL, kept between 1 and the last page.</summary>
    public static int Clamp(int? requested, int totalItems, int pageSize) =>
        Math.Clamp(requested ?? 1, 1, Math.Max(1, (totalItems + pageSize - 1) / pageSize));

    /// <summary>Route values for a link to <paramref name="page"/>; page 1 has no "page" value, so it has one address.</summary>
    public Dictionary<string, string> ValuesFor(int page)
    {
        var values = RouteValues
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value!);
        if (page > 1)
        {
            values["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return values;
    }
}
