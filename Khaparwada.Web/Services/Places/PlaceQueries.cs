using Khaparwada.Web.Data;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.Map;
using Khaparwada.Web.ViewModels.Places;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Places;

/// <summary>
/// Column selections shared by Home, the Places pages and the map, so a place looks the same everywhere.
/// "Gujarati, else English" is decided in SQL, so only one language leaves the database.
/// </summary>
public static class PlaceQueries
{
    public static IQueryable<PlaceCardViewModel> SelectCards(this IQueryable<Place> places, bool gujarati) =>
        places.Select(place => new PlaceCardViewModel
        {
            Slug = place.Slug,
            Name = gujarati ? place.NameGu ?? place.Name : place.Name,
            Summary = gujarati ? place.SummaryGu ?? place.Summary : place.Summary,
            CategoryName = gujarati ? place.Category.NameGu ?? place.Category.Name : place.Category.Name,
            CategoryIcon = place.Category.IconClass,
            CategoryColor = place.Category.MarkerColor,
            ImagePath = place.MainImagePath
        });

    /// <summary>Places that have coordinates, as map markers.</summary>
    public static IQueryable<MapMarkerViewModel> SelectMarkers(this IQueryable<Place> places, bool gujarati) =>
        places
            .Where(place => place.Latitude != null && place.Longitude != null)
            .Select(place => new MapMarkerViewModel
            {
                Slug = place.Slug,
                Name = gujarati ? place.NameGu ?? place.Name : place.Name,
                Summary = gujarati ? place.SummaryGu ?? place.Summary : place.Summary,
                Category = gujarati ? place.Category.NameGu ?? place.Category.Name : place.Category.Name,
                CategorySlug = place.Category.Slug,
                Icon = place.Category.IconClass,
                Color = place.Category.MarkerColor,
                ImagePath = place.MainImagePath,
                Latitude = place.Latitude!.Value,
                Longitude = place.Longitude!.Value
            });

    /// <summary>Category order first (as the admin arranged them), then by name.</summary>
    public static IOrderedQueryable<Place> InListOrder(this IQueryable<Place> places) =>
        places.OrderBy(place => place.Category.SortOrder).ThenBy(place => place.Name);

    /// <summary>
    /// The categories that are switched on, in the admin's order, each with its number of visible places.
    /// Used by Home (tiles and statistics), the Places filter and the map legend.
    /// </summary>
    public static async Task<List<PlaceCategoryCount>> LoadCategoriesAsync(this AppDbContext db, bool gujarati, CancellationToken cancellationToken)
    {
        var counts = await db.Places.AsNoTracking()
            .Visible()
            .GroupBy(place => place.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.CategoryId, group => group.Count, cancellationToken);

        var rows = await db.PlaceCategories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new
            {
                category.Id,
                category.Slug,
                Name = gujarati ? category.NameGu ?? category.Name : category.Name,
                category.IconClass,
                category.MarkerColor,
                category.ShowInStats
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PlaceCategoryCount(
                row.Id,
                new CategoryTileViewModel
                {
                    Slug = row.Slug,
                    Name = row.Name,
                    Icon = row.IconClass,
                    Color = row.MarkerColor,
                    PlaceCount = counts.GetValueOrDefault(row.Id)
                },
                row.ShowInStats))
            .ToList();
    }
}

/// <summary>A switched-on place category with its tile (name, icon, colour, number of places).</summary>
public sealed record PlaceCategoryCount(int Id, CategoryTileViewModel Tile, bool ShowInStats);
