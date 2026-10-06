using System.Globalization;

namespace Khaparwada.Web.Infrastructure.Localization;

/// <summary>Language of the page being rendered, and the "Gujarati with English fallback" rule.</summary>
public static class AppLanguage
{
    /// <summary>True while rendering a Gujarati page (the request culture is set per request from the URL).</summary>
    public static bool IsGujarati => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "gu";

    /// <summary>"gu" or "en", for the html lang attribute.</summary>
    public static string HtmlLang => IsGujarati ? "gu" : "en";

    /// <summary>The Gujarati text on Gujarati pages when there is one; otherwise the English text.</summary>
    public static string Pick(string english, string? gujarati) =>
        IsGujarati && !string.IsNullOrWhiteSpace(gujarati) ? gujarati : english;

    /// <inheritdoc cref="Pick(string, string?)"/>
    public static string? PickOptional(string? english, string? gujarati) =>
        IsGujarati && !string.IsNullOrWhiteSpace(gujarati) ? gujarati : english;
}
