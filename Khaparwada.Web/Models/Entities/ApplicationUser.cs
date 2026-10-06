using Microsoft.AspNetCore.Identity;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// An Admin or Editor login (table AspNetUsers). Accounts are switched off with
/// <see cref="IsActive"/> = false, never deleted, so "reviewed by" history keeps pointing at a real user.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
}
