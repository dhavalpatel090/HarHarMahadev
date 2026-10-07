using System.Globalization;
using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.History;
using Khaparwada.Web.Services.Media;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

public interface ITimelineAdminService
{
    Task<IReadOnlyList<TimelineListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<TimelineEntryInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the entry when input.Id is null, otherwise updates it.</summary>
    Task<OperationResult> SaveAsync(TimelineEntryInputModel input, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the entry leaves the website; the row and its photo stay recoverable.</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class TimelineAdminService(AppDbContext db, IImageUploadService images) : ITimelineAdminService
{
    private const string MediaFolder = "history";

    public async Task<IReadOnlyList<TimelineListItem>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.HistoryEntries.AsNoTracking()
            .InTimelineOrder()
            .Select(entry => new TimelineListItem(
                entry.Id, entry.SortYear, entry.YearLabel, entry.Title, entry.TitleGu, entry.ImagePath,
                entry.IsHighlighted, entry.IsPublished,
                entry.Title.StartsWith("[") || entry.YearLabel.StartsWith("[") || entry.Description.StartsWith("[")))
            .ToListAsync(cancellationToken);

    public async Task<TimelineEntryInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var entry = await db.HistoryEntries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entry is null
            ? null
            : new TimelineEntryInputModel
            {
                Id = entry.Id,
                SortYear = entry.SortYear,
                YearLabel = entry.YearLabel,
                YearLabelGu = entry.YearLabelGu,
                Title = entry.Title,
                TitleGu = entry.TitleGu,
                Description = entry.Description,
                DescriptionGu = entry.DescriptionGu,
                CurrentImageUrl = MediaUrls.Image(entry.ImagePath),
                IsHighlighted = entry.IsHighlighted,
                IsPublished = entry.IsPublished
            };
    }

    public async Task<OperationResult> SaveAsync(TimelineEntryInputModel input, CancellationToken cancellationToken = default)
    {
        HistoryEntry? entry = null;
        if (input.Id is { } id)
        {
            entry = await db.HistoryEntries.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entry is null)
            {
                return OperationResult.Failure(string.Empty, "This entry no longer exists.");
            }
        }

        var image = await ImageEdit.PrepareAsync(images, input.Image, input.RemoveImage, MediaFolder, cancellationToken);
        if (image.Failed)
        {
            return OperationResult.Failure(nameof(input.Image), image.Error!);
        }

        if (entry is null)
        {
            entry = new HistoryEntry();
            db.HistoryEntries.Add(entry);
        }

        var year = input.SortYear!.Value; // [Required] has already checked it
        entry.SortYear = year;
        // Visitors see the label; without one, the plain year. A label allows "1950s" or "Before 1947".
        entry.YearLabel = AdminText.Optional(input.YearLabel) ?? year.ToString(CultureInfo.InvariantCulture);
        entry.YearLabelGu = AdminText.Optional(input.YearLabelGu);
        entry.Title = input.Title.Trim();
        entry.TitleGu = AdminText.Optional(input.TitleGu);
        entry.Description = input.Description.Trim();
        entry.DescriptionGu = AdminText.Optional(input.DescriptionGu);
        entry.ImagePath = image.Apply(entry.ImagePath);
        entry.IsHighlighted = input.IsHighlighted;
        entry.IsPublished = input.IsPublished;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
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
        var entry = await db.HistoryEntries.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return OperationResult.Failure(string.Empty, "This entry no longer exists.");
        }

        db.HistoryEntries.Remove(entry); // becomes a soft delete (AuditSaveChangesInterceptor)
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
