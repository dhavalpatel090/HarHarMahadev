using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.History;
using Khaparwada.Web.Services.Media;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface IThenNowAdminService
{
    Task<IReadOnlyList<ThenNowListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<ThenNowInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the pair when input.Id is null, otherwise updates it.</summary>
    Task<OperationResult> SaveAsync(ThenNowInputModel input, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the pair leaves the website; the row and its photos stay recoverable.</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ThenNowAdminService(AppDbContext db, IImageUploadService images) : IThenNowAdminService
{
    private const string MediaFolder = "then-now";

    public async Task<IReadOnlyList<ThenNowListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.ThenNowComparisons.AsNoTracking()
            .InDisplayOrder()
            .Select(pair => new ThenNowListItem(
                pair.Id, pair.Title, pair.TitleGu, pair.ThenLabel, pair.NowLabel, pair.ThenImagePath, pair.NowImagePath,
                pair.SortOrder, pair.IsPublished))
            .ToListAsync(cancellationToken);

    public async Task<ThenNowInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var pair = await db.ThenNowComparisons.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return pair is null
            ? null
            : new ThenNowInputModel
            {
                Id = pair.Id,
                Title = pair.Title,
                TitleGu = pair.TitleGu,
                Caption = pair.Caption,
                CaptionGu = pair.CaptionGu,
                ThenLabel = pair.ThenLabel,
                ThenLabelGu = pair.ThenLabelGu,
                NowLabel = pair.NowLabel,
                NowLabelGu = pair.NowLabelGu,
                CurrentThenImageUrl = MediaUrls.Image(pair.ThenImagePath),
                CurrentNowImageUrl = MediaUrls.Image(pair.NowImagePath),
                SortOrder = pair.SortOrder,
                IsPublished = pair.IsPublished
            };
    }

    public async Task<OperationResult> SaveAsync(ThenNowInputModel input, CancellationToken cancellationToken = default)
    {
        ThenNowComparison? pair = null;
        if (input.Id is { } id)
        {
            pair = await db.ThenNowComparisons.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (pair is null)
            {
                return OperationResult.Failure(string.Empty, "This pair of photos no longer exists.");
            }
        }

        var thenImage = await ImageEdit.PrepareAsync(images, input.ThenImage, input.RemoveThenImage, MediaFolder, cancellationToken);
        var nowImage = await ImageEdit.PrepareAsync(images, input.NowImage, input.RemoveNowImage, MediaFolder, cancellationToken);

        var result = new OperationResult();
        if (thenImage.Failed)
        {
            result.AddError(nameof(input.ThenImage), thenImage.Error!);
        }

        if (nowImage.Failed)
        {
            result.AddError(nameof(input.NowImage), nowImage.Error!);
        }

        // The whole point is the comparison: on the website a pair needs both photos.
        var thenPath = thenImage.Peek(pair?.ThenImagePath);
        var nowPath = nowImage.Peek(pair?.NowImagePath);
        if (input.IsPublished && result.Succeeded && (thenPath is null || nowPath is null))
        {
            result.AddError(nameof(input.IsPublished),
                "Add both photos before showing this on the website, or switch off “Show on the website” for now.");
        }

        if (!result.Succeeded)
        {
            thenImage.SaveFailed();
            nowImage.SaveFailed();
            return result;
        }

        if (pair is null)
        {
            pair = new ThenNowComparison();
            db.ThenNowComparisons.Add(pair);
        }

        pair.Title = input.Title.Trim();
        pair.TitleGu = AdminText.Optional(input.TitleGu);
        pair.Caption = AdminText.Optional(input.Caption);
        pair.CaptionGu = AdminText.Optional(input.CaptionGu);
        pair.ThenLabel = input.ThenLabel.Trim();
        pair.ThenLabelGu = AdminText.Optional(input.ThenLabelGu);
        pair.NowLabel = input.NowLabel.Trim();
        pair.NowLabelGu = AdminText.Optional(input.NowLabelGu);
        pair.ThenImagePath = thenImage.Apply(pair.ThenImagePath);
        pair.NowImagePath = nowImage.Apply(pair.NowImagePath);
        pair.SortOrder = input.SortOrder;
        pair.IsPublished = input.IsPublished;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            thenImage.SaveFailed();
            nowImage.SaveFailed();
            throw;
        }

        thenImage.SaveSucceeded();
        nowImage.SaveSucceeded();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var pair = await db.ThenNowComparisons.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pair is null)
        {
            return OperationResult.Failure(string.Empty, "This pair of photos no longer exists.");
        }

        db.ThenNowComparisons.Remove(pair); // becomes a soft delete (AuditSaveChangesInterceptor)
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
