using System.ComponentModel.DataAnnotations;

namespace Khaparwada.Web.Areas.Admin.Models;

public sealed class LoginInputModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Page to return to after signing in. Only addresses on this site are followed (see AccountController).</summary>
    public string? ReturnUrl { get; set; }
}

public sealed class ChangePasswordInputModel
{
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Use at least 12 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type the new password again.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The two new passwords are not the same.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password again")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
