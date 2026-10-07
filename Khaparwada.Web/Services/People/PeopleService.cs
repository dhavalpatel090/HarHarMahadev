using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Localization;
using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.ViewModels.People;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.People;

public interface IPeopleService
{
    Task<PersonListViewModel> GetListAsync(string? category, string? search, int? page, CancellationToken cancellationToken = default);
    Task<PersonDetailsViewModel?> GetDetailsAsync(string slug, CancellationToken cancellationToken = default);
}

/// <summary>The public People pages. Every query goes through PublicVisibility (published + consent + category on).</summary>
public sealed class PeopleService(AppDbContext db) : IPeopleService
{
    public const int PageSize = 24;

    public async Task<PersonListViewModel> GetListAsync(string? category, string? search, int? page, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var categories = await LoadCategoriesAsync(gujarati, cancellationToken);
        var selected = categories.FirstOrDefault(c => string.Equals(c.Slug, category, StringComparison.OrdinalIgnoreCase));
        search = NormalizeSearch(search);

        var query = db.People.AsNoTracking().Visible();
        if (selected is not null)
        {
            query = query.Where(person => person.Category.Slug == selected.Slug);
        }

        if (search is not null)
        {
            query = query.Where(person =>
                person.FullName.Contains(search) || (person.FullNameGu != null && person.FullNameGu.Contains(search)) ||
                person.Profession.Contains(search) || (person.ProfessionGu != null && person.ProfessionGu.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var people = await query
            .OrderBy(person => person.Category.SortOrder)
            .ThenBy(person => person.FullName)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .SelectCards(gujarati)
            .ToListAsync(cancellationToken);

        return new PersonListViewModel
        {
            Categories = categories,
            Category = selected,
            Search = search,
            People = people,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = RouteNames.People,
                RouteValues = new Dictionary<string, string?> { ["category"] = selected?.Slug, ["q"] = search }
            }
        };
    }

    public async Task<PersonDetailsViewModel?> GetDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var gujarati = AppLanguage.IsGujarati;
        var person = await db.People.AsNoTracking()
            .Visible()
            .Where(p => p.Slug == slug)
            .Select(p => new
            {
                p.Id,
                p.Slug,
                p.CategoryId,
                FullName = gujarati ? p.FullNameGu ?? p.FullName : p.FullName,
                Profession = gujarati ? p.ProfessionGu ?? p.Profession : p.Profession,
                ShortBio = gujarati ? p.ShortBioGu ?? p.ShortBio : p.ShortBio,
                Achievement = gujarati ? p.AchievementGu ?? p.Achievement : p.Achievement,
                Contribution = gujarati ? p.ContributionGu ?? p.Contribution : p.Contribution,
                p.PhotoPath,
                CategorySlug = p.Category.Slug,
                CategoryName = gujarati ? p.Category.NameGu ?? p.Category.Name : p.Category.Name,
                p.Category.IconClass
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (person is null)
        {
            return null;
        }

        var more = await db.People.AsNoTracking()
            .Visible()
            .Where(p => p.CategoryId == person.CategoryId && p.Id != person.Id)
            .OrderBy(p => p.FullName)
            .Take(4)
            .SelectCards(gujarati)
            .ToListAsync(cancellationToken);

        return new PersonDetailsViewModel
        {
            Slug = person.Slug,
            FullName = person.FullName,
            Profession = person.Profession,
            ShortBio = person.ShortBio,
            Achievement = person.Achievement,
            Contribution = person.Contribution,
            PhotoPath = person.PhotoPath,
            Category = new PersonCategoryViewModel(person.CategorySlug, person.CategoryName, person.IconClass, 0),
            MorePeople = more
        };
    }

    private async Task<List<PersonCategoryViewModel>> LoadCategoriesAsync(bool gujarati, CancellationToken cancellationToken)
    {
        var counts = await db.People.AsNoTracking()
            .Visible()
            .GroupBy(person => person.CategoryId)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.CategoryId, group => group.Count, cancellationToken);

        var rows = await db.PersonCategories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new
            {
                category.Id,
                category.Slug,
                Name = gujarati ? category.NameGu ?? category.Name : category.Name,
                category.IconClass
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PersonCategoryViewModel(row.Slug, row.Name, row.IconClass, counts.GetValueOrDefault(row.Id))).ToList();
    }

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var text = string.Join(' ', search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length <= 100 ? text : text[..100];
    }
}

/// <summary>The person card selection, shared with Home.</summary>
public static class PeopleQueries
{
    public static IQueryable<PersonCardViewModel> SelectCards(this IQueryable<Person> people, bool gujarati) =>
        people.Select(person => new PersonCardViewModel
        {
            Slug = person.Slug,
            FullName = gujarati ? person.FullNameGu ?? person.FullName : person.FullName,
            Profession = gujarati ? person.ProfessionGu ?? person.Profession : person.Profession,
            CategoryName = gujarati ? person.Category.NameGu ?? person.Category.Name : person.Category.Name,
            CategoryIcon = person.Category.IconClass,
            PhotoPath = person.PhotoPath
        });
}
