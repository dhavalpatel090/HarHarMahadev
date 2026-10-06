namespace Khaparwada.Web.Models.Common;

/// <summary>
/// Base class for every domain table: an identity primary key plus audit timestamps.
/// CreatedAt and UpdatedAt are filled in by AuditSaveChangesInterceptor; never set them by hand.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
