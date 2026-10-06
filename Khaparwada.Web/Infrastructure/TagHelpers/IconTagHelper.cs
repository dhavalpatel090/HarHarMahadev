using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Khaparwada.Web.Infrastructure.TagHelpers;

/// <summary>
/// Renders an icon from the site's SVG sprite (built by tools/build-icon-sprite.ps1):
/// <c>&lt;icon name="gopuram" /&gt;</c>, <c>&lt;icon brand="whatsapp" /&gt;</c>, or a stored Font Awesome
/// class such as <c>&lt;icon name="fa-solid fa-gopuram" /&gt;</c>.
/// Icons are decorative (hidden from screen readers) unless <c>label</c> is set.
/// The colour is an SVG presentation attribute, not an inline style, so a strict
/// Content-Security-Policy (Phase 13) does not block it.
/// </summary>
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
public sealed class IconTagHelper(IFileVersionProvider fileVersionProvider) : TagHelper
{
    public const string SpritePath = "/images/icons.svg";

    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Label { get; set; }
    public string? Color { get; set; }
    public string? Class { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var symbol = string.IsNullOrWhiteSpace(Brand) ? IconNames.ToSymbol(Name) : "brand-" + Brand;

        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", string.IsNullOrWhiteSpace(Class) ? "icon" : "icon " + Class);
        output.Attributes.SetAttribute("focusable", "false");

        if (string.IsNullOrWhiteSpace(Label))
        {
            output.Attributes.SetAttribute("aria-hidden", "true");
        }
        else
        {
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", Label);
        }

        if (IconNames.IsHexColor(Color))
        {
            output.Attributes.SetAttribute("color", Color);
        }

        // PathString.Empty: the sprite URL must not get the "/gu" prefix, or each language would cache its own copy.
        var href = fileVersionProvider.AddFileVersionToPath(PathString.Empty, SpritePath) + "#" + symbol;
        output.Content.SetHtmlContent($"<use href=\"{HtmlEncoder.Default.Encode(href)}\"></use>");
    }
}
