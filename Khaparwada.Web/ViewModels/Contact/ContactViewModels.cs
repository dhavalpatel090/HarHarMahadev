using System.ComponentModel.DataAnnotations;
using Khaparwada.Web.Models.Common;
using Khaparwada.Web.ViewModels.Map;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.Contact;

/// <summary>/contact: the village's details, the map, useful links and the message form.</summary>
public sealed class ContactPageViewModel
{
    public required SiteInfo Site { get; init; }
    public MapViewModel? Map { get; init; }
    public required IReadOnlyList<ExternalLink> UsefulLinks { get; init; }
    public required ContactFormModel Form { get; init; }
}

/// <summary>
/// The message form. Validation messages are localization keys (English text). Nothing about the sender is
/// stored beyond what they type here: no network address, no browser details.
/// </summary>
public sealed class ContactFormModel
{
    [Required(ErrorMessage = "Please write your name.")]
    [StringLength(FieldLengths.Name, ErrorMessage = "Please keep your name under {1} characters.")]
    [Display(Name = "Your name")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Please check the email address.")]
    [StringLength(FieldLengths.Email)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [RegularExpression(@"^\+?[0-9 ()\-]{6,20}$", ErrorMessage = "Please check the phone number: digits, spaces, + and - only.")]
    [StringLength(FieldLengths.Phone)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Please write what your message is about.")]
    [StringLength(FieldLengths.Title, ErrorMessage = "Please keep the subject under {1} characters.")]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write your message.")]
    [StringLength(FieldLengths.ContactMessage, MinimumLength = 10, ErrorMessage = "Please write between {2} and {1} characters.")]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The "honeypot": hidden from people (and from screen readers), but spam robots fill in every field. A
    /// message with this filled in is thanked for and thrown away, so the robot learns nothing.
    /// </summary>
    public string? Website { get; set; }
}
