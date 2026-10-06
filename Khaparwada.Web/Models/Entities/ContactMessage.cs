using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>A message from the Contact form. Admin-only; nothing here is ever shown publicly.</summary>
public class ContactMessage : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
