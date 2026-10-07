using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.ViewModels.About;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface IAboutSectionAdminService
{
    Task<IReadOnlyList<AboutSectionListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<AboutSectionInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the section when input.Id is null, otherwise updates it.</summary>
    Task<OperationResult> SaveAsync(AboutSectionInputModel input, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the section leaves the website; the row and its photo stay recoverable.</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class AboutSectionAdminService(AppDbContext db, IImageUploadService images) : IAboutSectionAdminService
{
    private const string MediaFolder = "about";

    public async Task<IReadOnlyList<AboutSectionListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.AboutSections.AsNoTracking()
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Id)
            .Select(section => new AboutSectionListItem(
                section.Id, section.Title, section.TitleGu, section.Slug, section.ImagePath, section.SortOrder,
                section.IsPublished, section.Body.StartsWith("["), section.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<AboutSectionInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var section = await db.AboutSections.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        return section is null
            ? null
            : new AboutSectionInputModel
            {
                Id = section.Id,
                Title = section.Title,
                TitleGu = section.TitleGu,
                Slug = section.Slug,
                Body = section.Body,
                BodyGu = section.BodyGu,
                CurrentImageUrl = MediaUrls.Image(section.ImagePath),
                SortOrder = section.SortOrder,
                IsPublished = section.IsPublished
            };
    }

    public async Task<OperationResult> SaveAsync(AboutSectionInputModel input, CancellationToken cancellationToken = default)
    {
        // An empty web address is made from the English title.
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Create(input.Title) : input.Slug.Trim();
        if (!SlugGenerator.IsValid(slug))
        {
            return OperationResult.Failure(nameof(input.Slug), "Enter a web address in English letters, e.g. village-life.");
        }

        if (AboutPageAnchors.IsReserved(slug))
        {
            return OperationResult.Failure(nameof(input.Slug), "The About page already uses this web address for itself. Choose another one.");
        }

        if (await db.AboutSections.AnyAsync(s => s.Slug == slug && s.Id != input.Id, cancellationToken))
        {
            return OperationResult.Failure(nameof(input.Slug), "Another section already uses this web address.");
        }

        AboutSection? section = null;
        if (input.Id is { } id)
        {
            section = await db.AboutSections.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (section is null)
            {
                return OperationResult.Failure(string.Empty, "This section no longer exists.");
            }
        }

        var image = await ImageEdit.PrepareAsync(images, input.Image, input.RemoveImage, MediaFolder, cancellationToken);
        if (image.Failed)
        {
            return OperationResult.Failure(nameof(input.Image), image.Error!);
        }

        if (section is null)
        {
            section = new AboutSection();
            db.AboutSections.Add(section);
        }

        section.Title = input.Title.Trim();
        section.TitleGu = AdminText.Optional(input.TitleGu);
        section.Slug = slug;
        section.Body = input.Body.Trim();
        section.BodyGu = AdminText.Optional(input.BodyGu);
        section.ImagePath = image.Apply(section.ImagePath);
        section.SortOrder = input.SortOrder;
        section.IsPublished = input.IsPublished;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two admins saving the same web address at the same moment: the unique index catches it.
            image.SaveFailed();
            return OperationResult.Failure(nameof(input.Slug), "Another section already uses this web address.");
        }
        catch
        {
            image.SaveFailed();
            throw;
        }

        image.SaveSucceeded();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var section = await db.AboutSections.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (section is null)
        {
            return OperationResult.Failure(string.Empty, "This section no longer exists.");
        }

        db.AboutSections.Remove(section); // becomes a soft delete (AuditSaveChangesInterceptor)
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
