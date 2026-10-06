using System.ComponentModel.DataAnnotations;

namespace Khaparwada.Web.Infrastructure.Validation;

/// <summary>
/// An absolute http:// or https:// address. [Url] alone also accepts ftp:// and does not stop other schemes
/// in every case; a "javascript:" link in an href would run script when clicked. The database has the same
/// rule as a CHECK constraint.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class HttpUrlAttribute() : ValidationAttribute("Enter a full web address starting with https:// or http://.")
{
    public override bool IsValid(object? value) =>
        value is null
        || value is string { Length: 0 }
        || (value is string text
            && Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));
}
