using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Data;
using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.Models.Entities;
using Khaparwada.Web.Models.Enums;
using Khaparwada.Web.Services.Common;
using Khaparwada.Web.Services.Media;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.EntityFrameworkCore;

namespace Khaparwada.Web.Services.Admin;

/// <summary>Who is saving: the review rules differ for Admins and Editors.</summary>
public sealed record AdminActor(string UserId, bool IsAdmin);

/// <summary>The result of a save, with the review status before (null for a new profile) and after it.</summary>
public sealed record PersonSaveOutcome(OperationResult Result, ModerationStatus? Before, ModerationStatus After)
{
    /// <summary>The profile was on the website and no longer is (its changes wait for an Admin).</summary>
    public bool LeftWebsite => Before == ModerationStatus.Published && After != ModerationStatus.Published;
}

public interface IPersonAdminService
{
    Task<PersonAdminListViewModel> ListAsync(PersonListFilter filter, int? categoryId, string? search, int? page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PersonInputModel?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Creates the profile when input.Id is null, otherwise updates it, applying the review rules.</summary>
    Task<PersonSaveOutcome> SaveAsync(PersonInputModel input, AdminActor actor, CancellationToken cancellationToken = default);

    /// <summary>Soft delete: the profile leaves the website at once (also the answer to a removal request).</summary>
    Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Community profiles and their review. The rules, all here:
/// <list type="bullet">
/// <item>Only Admins decide (approve, show, hide, send back); Editors write and change drafts.</item>
/// <item>Approved and Shown need recorded consent, with a note of how it was given (the database also refuses
/// Approved/Published without consent).</item>
/// <item>"Sent back" needs a note saying why.</item>
/// <item>When an Editor changes anything visitors would see, or the consent, the profile goes back to "Waiting
/// for review" and leaves the website until an Admin approves it again: no personal detail reaches the public
/// without a second person checking it.</item>
/// </list>
/// </summary>
public sealed class PersonAdminService(AppDbContext db, IImageUploadService images, TimeProvider timeProvider) : IPersonAdminService
{
    public const int PageSize = 25;
    private const string MediaFolder = "people";
    private const string ListRouteName = "admin-people";

    public async Task<PersonAdminListViewModel> ListAsync(
        PersonListFilter filter, int? categoryId, string? search, int? page, CancellationToken cancellationToken = default)
    {
        search = AdminText.Optional(search);
        var query = db.People.AsNoTracking();
        if (categoryId is { } id)
        {
            query = query.Where(person => person.CategoryId == id);
        }

        if (search is not null)
        {
            query = query.Where(person => person.FullName.Contains(search) || (person.FullNameGu != null && person.FullNameGu.Contains(search)));
        }

        // One grouped query gives every tab its count (for the same category and search).
        var byStatus = await query
            .GroupBy(person => person.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);
        int CountOf(params ModerationStatus[] statuses) => statuses.Sum(status => byStatus.GetValueOrDefault(status));
        var counts = new Dictionary<PersonListFilter, int>
        {
            [PersonListFilter.All] = byStatus.Values.Sum(),
            [PersonListFilter.Review] = CountOf(ModerationStatus.Pending, ModerationStatus.UnderReview),
            [PersonListFilter.Published] = CountOf(ModerationStatus.Published),
            [PersonListFilter.Hidden] = CountOf(ModerationStatus.Approved),
            [PersonListFilter.Rejected] = CountOf(ModerationStatus.Rejected)
        };

        query = filter switch
        {
            PersonListFilter.Review => query.Where(p => p.Status == ModerationStatus.Pending || p.Status == ModerationStatus.UnderReview),
            PersonListFilter.Published => query.Where(p => p.Status == ModerationStatus.Published),
            PersonListFilter.Hidden => query.Where(p => p.Status == ModerationStatus.Approved),
            PersonListFilter.Rejected => query.Where(p => p.Status == ModerationStatus.Rejected),
            _ => query
        };

        var total = counts[filter];
        var currentPage = PaginationViewModel.Clamp(page, total, PageSize);
        var items = await query
            .OrderBy(person => person.FullName)
            .ThenBy(person => person.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(person => new PersonListItem(
                person.Id, person.FullName, person.FullNameGu, person.Profession, person.Category.Name, person.PhotoPath,
                person.HasConsent, person.IsFeatured, person.Status,
                person.FullName.StartsWith("[") || person.ShortBio.StartsWith("[")))
            .ToListAsync(cancellationToken);

        return new PersonAdminListViewModel
        {
            Items = items,
            Categories = await GetCategoriesAsync(cancellationToken),
            Counts = counts,
            Filter = filter,
            CategoryId = categoryId,
            Search = search,
            Pagination = new PaginationViewModel
            {
                Page = currentPage,
                PageSize = PageSize,
                TotalItems = total,
                RouteName = ListRouteName,
                RouteValues = new Dictionary<string, string?>
                {
                    ["status"] = filter == PersonListFilter.All ? null : filter.ToString().ToLowerInvariant(),
                    ["category"] = categoryId?.ToString(),
                    ["q"] = search
                }
            }
        };
    }

    public async Task<IReadOnlyList<PlaceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await db.PersonCategories.AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new PlaceCategoryOption(category.Id, category.Name, category.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<PersonInputModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await db.People.AsNoTracking()
            .Where(person => person.Id == id)
            .Select(person => new
            {
                Person = person,
                Reviewer = person.ReviewedBy == null ? null
                    : person.ReviewedBy.DisplayName != "" ? person.ReviewedBy.DisplayName : person.ReviewedBy.Email
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var p = row.Person;
        return new PersonInputModel
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            FullName = p.FullName,
            FullNameGu = p.FullNameGu,
            Slug = p.Slug,
            Profession = p.Profession,
            ProfessionGu = p.ProfessionGu,
            ShortBio = p.ShortBio,
            ShortBioGu = p.ShortBioGu,
            Achievement = p.Achievement,
            AchievementGu = p.AchievementGu,
            Contribution = p.Contribution,
            ContributionGu = p.ContributionGu,
            CurrentPhotoUrl = MediaUrls.Image(p.PhotoPath),
            HasConsent = p.HasConsent,
            ConsentNote = p.ConsentNote,
            IsFeatured = p.IsFeatured,
            Status = p.Status,
            ReviewNote = p.ReviewNote,
            CurrentStatus = p.Status,
            ConsentRecordedAt = p.ConsentRecordedAt,
            ReviewedBy = row.Reviewer,
            ReviewedAt = p.ReviewedAt
        };
    }

    public async Task<PersonSaveOutcome> SaveAsync(PersonInputModel input, AdminActor actor, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Create(input.FullName) : input.Slug.Trim();
        if (!SlugGenerator.IsValid(slug))
        {
            return Failed(nameof(input.Slug), "Enter a web address in English letters, e.g. ramesh-patel.");
        }

        if (await db.People.AnyAsync(p => p.Slug == slug && p.Id != input.Id, cancellationToken))
        {
            return Failed(nameof(input.Slug), "Another profile already uses this web address.");
        }

        if (!await db.PersonCategories.AnyAsync(c => c.Id == input.CategoryId, cancellationToken))
        {
            return Failed(nameof(input.CategoryId), "Choose a category from the list.");
        }

        var consentNote = AdminText.Optional(input.ConsentNote);
        if (input.HasConsent && consentNote is null)
        {
            // A ticked box alone cannot answer "who agreed, and how?" if the person asks later.
            return Failed(nameof(input.ConsentNote), "Write how consent was given, e.g. “Agreed in person on 5 Oct 2026”.");
        }

        Person? person = null;
        if (input.Id is { } id)
        {
            person = await db.People.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (person is null)
            {
                return Failed(string.Empty, "This profile no longer exists.");
            }
        }

        // An Admin's decision is checked before anything is written, so a refused one leaves no trace.
        var reviewNote = AdminText.Optional(input.ReviewNote);
        ModerationStatus? decision = actor.IsAdmin ? input.Status ?? person?.Status ?? ModerationStatus.Pending : null;
        if (decision is ModerationStatus.Approved or ModerationStatus.Published && !input.HasConsent)
        {
            return Failed(nameof(input.Status), "A profile can only be approved or shown with the person's consent. Tick the consent box, or choose another decision.");
        }

        if (decision is ModerationStatus.Rejected && reviewNote is null)
        {
            return Failed(nameof(input.ReviewNote), "Write a note saying why, so the profile can be put right.");
        }

        var photo = await ImageEdit.PrepareAsync(images, input.Photo, input.RemovePhoto, MediaFolder, cancellationToken);
        if (photo.Failed)
        {
            return Failed(nameof(input.Photo), photo.Error!);
        }

        ModerationStatus? before = person?.Status;
        if (person is null)
        {
            person = new Person();
            db.People.Add(person);
        }

        var shownBefore = Snapshot(person);
        var now = timeProvider.GetUtcNow();

        person.CategoryId = input.CategoryId!.Value;
        person.FullName = input.FullName.Trim();
        person.FullNameGu = AdminText.Optional(input.FullNameGu);
        person.Slug = slug;
        person.Profession = input.Profession.Trim();
        person.ProfessionGu = AdminText.Optional(input.ProfessionGu);
        person.ShortBio = input.ShortBio.Trim();
        person.ShortBioGu = AdminText.Optional(input.ShortBioGu);
        person.Achievement = AdminText.Optional(input.Achievement);
        person.AchievementGu = AdminText.Optional(input.AchievementGu);
        person.Contribution = AdminText.Optional(input.Contribution);
        person.ContributionGu = AdminText.Optional(input.ContributionGu);
        person.PhotoPath = photo.Apply(person.PhotoPath);
        person.IsFeatured = input.IsFeatured;

        // The time is when consent was ticked; unticking clears it, so a new agreement gets a new time.
        if (!input.HasConsent)
        {
            person.ConsentRecordedAt = null;
        }
        else if (!person.HasConsent || person.ConsentRecordedAt is null)
        {
            person.ConsentRecordedAt = now;
        }

        person.HasConsent = input.HasConsent;
        person.ConsentNote = consentNote;

        if (decision is { } status)
        {
            // An Admin's decision; approving, showing and sending back record who decided and when.
            if (status is ModerationStatus.Approved or ModerationStatus.Published or ModerationStatus.Rejected && status != before)
            {
                person.ReviewedByUserId = actor.UserId;
                person.ReviewedAt = now;
            }

            person.Status = status;
            person.ReviewNote = reviewNote;
        }
        else if (before is null || Snapshot(person) != shownBefore)
        {
            // An Editor's new profile or change: an Admin looks at it before visitors do.
            person.Status = ModerationStatus.Pending;
        }

        // Safety net (the database check constraint is the last one): no consent, never approved or shown.
        if (!person.HasConsent && person.Status is ModerationStatus.Approved or ModerationStatus.Published)
        {
            person.Status = ModerationStatus.Pending;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two people saving the same web address at the same moment: the unique index catches it.
            photo.SaveFailed();
            return Failed(nameof(input.Slug), "Another profile already uses this web address.");
        }
        catch
        {
            photo.SaveFailed();
            throw;
        }

        photo.SaveSucceeded();
        return new PersonSaveOutcome(OperationResult.Success(), before, person.Status);
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var person = await db.People.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
        {
            return OperationResult.Failure(string.Empty, "This profile no longer exists.");
        }

        db.People.Remove(person); // becomes a soft delete (AuditSaveChangesInterceptor)
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static PersonSaveOutcome Failed(string field, string message) =>
        new(OperationResult.Failure(field, message), null, ModerationStatus.Pending);

    /// <summary>Everything visitors would see, plus the consent: a change to any of it needs a new review.</summary>
    private static string Snapshot(Person person) => string.Join('\u001f',
        person.CategoryId, person.FullName, person.FullNameGu, person.Slug, person.Profession, person.ProfessionGu,
        person.ShortBio, person.ShortBioGu, person.Achievement, person.AchievementGu, person.Contribution,
        person.ContributionGu, person.PhotoPath, person.HasConsent, person.ConsentNote);
}
