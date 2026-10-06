using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Khaparwada.Web.Infrastructure.TagHelpers;

/// <summary>
/// One form field typed in both languages, side by side:
/// <c>&lt;bilingual-field for="Name" for-gu="NameGu" /&gt;</c>, or with <c>rows="4"</c> for a text area.
/// Renders a fieldset named after the field, an "English" and a "ગુજરાતી" box with their validation messages,
/// and an optional hint. Every admin form uses it, so all bilingual fields behave the same: the Gujarati box
/// has lang="gu" (right font and spell-checker), the maximum length matches the database, and the hint is
/// read out by screen readers.
/// </summary>
[HtmlTargetElement("bilingual-field", TagStructure = TagStructure.WithoutEndTag)]
public sealed class BilingualFieldTagHelper(IHtmlGenerator generator) : TagHelper
{
    [HtmlAttributeName("for")]
    public ModelExpression For { get; set; } = default!;

    [HtmlAttributeName("for-gu")]
    public ModelExpression ForGu { get; set; } = default!;

    /// <summary>0 (the default) for a one-line box; otherwise the height of a text area, in lines.</summary>
    public int Rows { get; set; }

    /// <summary>Optional help text shown under the field name.</summary>
    public string? Hint { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "fieldset";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("kc-bilingual", HtmlEncoder.Default);

        var legend = new TagBuilder("legend");
        legend.AddCssClass("kc-field-label");
        legend.InnerHtml.Append(For.Metadata.DisplayName ?? For.Name);
        output.Content.AppendHtml(legend);

        string? hintId = null;
        if (!string.IsNullOrWhiteSpace(Hint))
        {
            hintId = TagBuilder.CreateSanitizedId(For.Name + "-hint", "-");
            var hint = new TagBuilder("p");
            hint.AddCssClass("kc-field-hint");
            hint.Attributes["id"] = hintId;
            hint.InnerHtml.Append(Hint);
            output.Content.AppendHtml(hint);
        }

        var grid = new TagBuilder("div");
        grid.AddCssClass("kc-bilingual-grid");
        grid.InnerHtml.AppendHtml(Column(For, "English", lang: null, hintId));
        grid.InnerHtml.AppendHtml(Column(ForGu, "ગુજરાતી", lang: "gu", hintId));
        output.Content.AppendHtml(grid);
    }

    private IHtmlContent Column(ModelExpression field, string languageName, string? lang, string? hintId)
    {
        var column = new TagBuilder("div");
        column.AddCssClass("kc-bilingual-col");

        var label = generator.GenerateLabel(ViewContext, field.ModelExplorer, field.Name, languageName,
            new { @class = "kc-lang-label" });
        if (!field.Metadata.IsRequired)
        {
            label.InnerHtml.AppendHtml(" <span class=\"kc-optional\">optional</span>");
        }

        var attributes = new Dictionary<string, object?> { ["class"] = "form-control" };
        if (lang is not null)
        {
            attributes["lang"] = lang;
        }

        if (hintId is not null)
        {
            attributes["aria-describedby"] = hintId;
        }

        // The browser then stops typing at the database limit instead of the server rejecting it later.
        if (field.Metadata.ValidatorMetadata.OfType<StringLengthAttribute>().FirstOrDefault() is { } length)
        {
            attributes["maxlength"] = length.MaximumLength;
        }

        var input = Rows > 0
            ? generator.GenerateTextArea(ViewContext, field.ModelExplorer, field.Name, Rows, columns: 0, attributes)
            : generator.GenerateTextBox(ViewContext, field.ModelExplorer, field.Name, field.Model, format: null, attributes);

        var message = generator.GenerateValidationMessage(ViewContext, field.ModelExplorer, field.Name,
            message: null, tag: "div", htmlAttributes: new { @class = "kc-field-error" });

        column.InnerHtml.AppendHtml(label);
        column.InnerHtml.AppendHtml(input);
        if (message is not null)
        {
            column.InnerHtml.AppendHtml(message);
        }

        return column;
    }
}
