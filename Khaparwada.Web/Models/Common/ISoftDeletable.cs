namespace Khaparwada.Web.Models.Common;

/// <summary>
/// An entity whose rows are hidden instead of removed when deleted.
/// AuditSaveChangesInterceptor turns a Remove() into IsDeleted = true, and the entity's query
/// filter (see its configuration class) hides deleted rows from every query.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
