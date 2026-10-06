namespace Khaparwada.Web.Models.Enums;

/// <summary>
/// How prominently a notice is shown. Active <see cref="Urgent"/> notices also appear in the
/// site-wide alert bar. Stored as text.
/// </summary>
public enum NoticePriority
{
    Normal,
    Important,
    Urgent
}
