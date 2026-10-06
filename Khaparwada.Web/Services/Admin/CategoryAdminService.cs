using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.TagHelpers;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface ICategoryAdminService
{
    Task<IReadOnlyList<CategoryListItem>> ListAsync(CategoryKind kind, CancellationToken cancellationToken = default);
    Task<CategoryInputModel?> GetAsync(CategoryKind kind, int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the category when input.Id is null, otherwise updates it.</summary>
    Task<OperationResult> SaveAsync(CategoryInputModel input, CancellationToken cancellationToken = default);

    Task<CategoryDeleteViewModel?> GetDeleteInfoAsync(CategoryKind kind, int id, CancellationToken cancellationToken = default);

    /// <summary>Deletes a category that nothing uses. A category in use can only be switched off.</summary>
    Task<OperationResult> DeleteAsync(CategoryKind kind, int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Admin for the three category lists. The shared parts (name, web address, order, on/off) are written once
/// against <see cref="ICategory"/>; icon, marker colour and statistics apply to some kinds only.
/// </summary>
public sealed class CategoryAdminService(AppDbContext db, IIconCatalog icons) : ICategoryAdminService
{
    public async Task<IReadOnlyList<CategoryListItem>> ListAsync(CategoryKind kind, CancellationToken cancellationToken = default)
    {
        // Usage counts include deleted items still in the bin (IgnoreQueryFilters), because they still hold the category.
        return kind switch
        {
            CategoryKind.Places => await db.PlaceCategories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryListItem(c.Id, c.Name, c.NameGu, c.Slug, c.IconClass, c.MarkerColor, c.ShowInStats,
                    c.SortOrder, c.IsActive, db.Places.IgnoreQueryFilters().Count(p => p.CategoryId == c.Id)))
                .ToListAsync(cancellationToken),
            CategoryKind.People => await db.PersonCategories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryListItem(c.Id, c.Name, c.NameGu, c.Slug, c.IconClass, null, false,
                    c.SortOrder, c.IsActive, db.People.IgnoreQueryFilters().Count(p => p.CategoryId == c.Id)))
                .ToListAsync(cancellationToken),
            _ => await db.GalleryCategories.AsNoTracking()
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .Select(c => new CategoryListItem(c.Id, c.Name, c.NameGu, c.Slug, null, null, false,
                    c.SortOrder, c.IsActive, db.GalleryAlbums.IgnoreQueryFilters().Count(a => a.CategoryId == c.Id)))
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<CategoryInputModel?> GetAsync(CategoryKind kind, int id, CancellationToken cancellationToken = default)
    {
        var category = await FindAsync(kind, id, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var model = new CategoryInputModel
        {
            Kind = kind,
            Id = id,
            Name = category.Name,
            NameGu = category.NameGu,
            Slug = category.Slug,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };

        switch (category)
        {
            case PlaceCategory place:
                model.Icon = IconNames.ToSymbol(place.IconClass);
                model.MarkerColor = place.MarkerColor;
                model.ShowInStats = place.ShowInStats;
                break;
            case PersonCategory person:
                model.Icon = IconNames.ToSymbol(person.IconClass);
                break;
        }

        return model;
    }

    public async Task<OperationResult> SaveAsync(CategoryInputModel input, CancellationToken cancellationToken = default)
    {
        // An empty web address is made from the English name.
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Create(input.Name) : input.Slug.Trim();
        if (!SlugGenerator.IsValid(slug))
        {
            return OperationResult.Failure(nameof(input.Slug), "Enter a web address in English letters, e.g. water-places.");
        }

        if (input.HasIcon && !icons.IsCategoryIcon(input.Icon))
        {
            return OperationResult.Failure(nameof(input.Icon), "Choose an icon from the list.");
        }

        var iconClass = $"fa-solid fa-{input.Icon}";
        return input.Kind switch
        {
            CategoryKind.Places => await SaveAsync(db.PlaceCategories, input, slug, category =>
            {
                category.IconClass = iconClass;
                category.MarkerColor = input.MarkerColor.ToUpperInvariant();
                category.ShowInStats = input.ShowInStats;
            }, cancellationToken),
            CategoryKind.People => await SaveAsync(db.PersonCategories, input, slug,
                category => category.IconClass = iconClass, cancellationToken),
            _ => await SaveAsync(db.GalleryCategories, input, slug, _ => { }, cancellationToken)
        };
    }

    public async Task<CategoryDeleteViewModel?> GetDeleteInfoAsync(CategoryKind kind, int id, CancellationToken cancellationToken = default)
    {
        var category = await FindAsync(kind, id, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var usage = kind switch
        {
            CategoryKind.Places => await db.Places.IgnoreQueryFilters().CountAsync(p => p.CategoryId == id, cancellationToken),
            CategoryKind.People => await db.People.IgnoreQueryFilters().CountAsync(p => p.CategoryId == id, cancellationToken),
            _ => await db.GalleryAlbums.IgnoreQueryFilters().CountAsync(a => a.CategoryId == id, cancellationToken)
        };
        return new CategoryDeleteViewModel(kind, id, category.Name, usage);
    }

    public async Task<OperationResult> DeleteAsync(CategoryKind kind, int id, CancellationToken cancellationToken = default)
    {
        var info = await GetDeleteInfoAsync(kind, id, cancellationToken);
        if (info is null)
        {
            return OperationResult.Failure(string.Empty, "This category no longer exists.");
        }

        if (info.UsageCount > 0)
        {
            return OperationResult.Failure(string.Empty,
                $"“{info.Name}” is used by {CategoryKinds.CountOf(kind, info.UsageCount)}. Move them to another category first, or switch this one off instead.");
        }

        _ = kind switch
        {
            CategoryKind.Places => await db.PlaceCategories.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken),
            CategoryKind.People => await db.PersonCategories.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken),
            _ => await db.GalleryCategories.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken)
        };
        return OperationResult.Success();
    }

    private async Task<OperationResult> SaveAsync<T>(
        DbSet<T> categories, CategoryInputModel input, string slug, Action<T> applyKindFields, CancellationToken cancellationToken)
        where T : class, ICategory, new()
    {
        if (await categories.AnyAsync(c => c.Slug == slug && c.Id != input.Id, cancellationToken))
        {
            return OperationResult.Failure(nameof(input.Slug), "Another category already uses this web address.");
        }

        T? category;
        if (input.Id is { } id)
        {
            category = await categories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (category is null)
            {
                return OperationResult.Failure(string.Empty, "This category no longer exists.");
            }
        }
        else
        {
            category = new T();
            categories.Add(category);
        }

        category.Name = input.Name.Trim();
        category.NameGu = string.IsNullOrWhiteSpace(input.NameGu) ? null : input.NameGu.Trim();
        category.Slug = slug;
        category.SortOrder = input.SortOrder;
        category.IsActive = input.IsActive;
        applyKindFields(category);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two admins saving the same web address at the same moment: the unique index catches it.
            return OperationResult.Failure(nameof(input.Slug), "Another category already uses this web address.");
        }

        return OperationResult.Success();
    }

    private async Task<ICategory?> FindAsync(CategoryKind kind, int id, CancellationToken cancellationToken) => kind switch
    {
        CategoryKind.Places => await db.PlaceCategories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken),
        CategoryKind.People => await db.PersonCategories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken),
        _ => await db.GalleryCategories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
    };
}
