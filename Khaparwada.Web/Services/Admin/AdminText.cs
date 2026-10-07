namespace Khaparwada.Web.Services.Admin;

/// <summary>How admin services store typed text.</summary>
public static class AdminText
{
    /// <summary>
    /// Trimmed text, or null for an empty field. Optional Gujarati fields must be null rather than "" so that
    /// "Gujarati, else English" (<c>TitleGu ?? Title</c>) falls back to English.
    /// </summary>
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
