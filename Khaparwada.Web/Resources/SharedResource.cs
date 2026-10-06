namespace Khaparwada.Web;

/// <summary>
/// Marker type for the site's shared UI text: views use <c>@L["Upcoming events"]</c>
/// (IStringLocalizer&lt;SharedResource&gt;, injected in Views/_ViewImports.cshtml).
/// The keys are the English text, so a missing translation shows English instead of a key.
/// Gujarati translations go in Resources/SharedResource.gu.resx (Phase 12).
/// The class is in the root namespace on purpose: the .resx name is derived from the namespace, and
/// this keeps the file at Resources/SharedResource.gu.resx.
/// </summary>
public sealed class SharedResource
{
}
