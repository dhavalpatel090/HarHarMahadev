namespace Khaparwada.Web.Infrastructure.Localization;

/// <summary>
/// Turns "/gu/places/x" into PathBase "/gu" + Path "/places/x" and marks the request as Gujarati.
/// Routing then sees the same path in both languages (one set of routes, no duplicated controllers), and
/// every link ASP.NET Core generates on a Gujarati page starts with "/gu" automatically, because link
/// generation always prefixes the PathBase. A "{culture}" route parameter would not work as reliably:
/// ASP.NET Core drops it from links that point to a different controller.
/// </summary>
public sealed class LanguagePrefixMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        if (request.Path.StartsWithSegments(SiteLanguages.GujaratiPathPrefix, StringComparison.OrdinalIgnoreCase, out var rest))
        {
            // The admin area is English-only; "/gu/admin/..." is not a page.
            if (rest.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            request.PathBase = request.PathBase.Add(SiteLanguages.GujaratiPathPrefix);
            request.Path = rest.HasValue ? rest : "/";
            context.Items[SiteLanguages.GujaratiRequestItemKey] = true;
        }

        return next(context);
    }
}
