using Khaparwada.Web.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Khaparwada.Web.Data.Interceptors;

/// <summary>
/// Runs just before every SaveChanges and applies the audit rules in one place, so no service has to
/// remember them:
/// <list type="bullet">
/// <item>new rows get CreatedAt and UpdatedAt; changed rows get a new UpdatedAt and CreatedAt is locked;</item>
/// <item>deleting a soft-deletable row becomes an update that sets IsDeleted and DeletedAt.</item>
/// </list>
/// </summary>
public sealed class AuditSaveChangesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();

        // ToList(): entry states are changed inside the loop, so iterate over a snapshot.
        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable softDeletable })
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
            }

            if (entry.Entity is not BaseEntity entity)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = now;
                entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
            }
        }
    }
}
