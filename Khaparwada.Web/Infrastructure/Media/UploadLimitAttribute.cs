using Khaparwada.Web.Infrastructure.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Infrastructure.Media;

/// <summary>
/// Sets an upload form's request-size limit from the Storage settings: <c>[UploadLimit(images: 2)]</c> allows two
/// photos of MaxImageMegabytes each (plus 1 MB for the other fields). The server's default limit is about 28 MB,
/// so without this a form with two photos, or a raised MaxImageMegabytes, would fail with "413 Request too large",
/// and a one-photo form would accept far more than it needs. On IIS the site's web.config limit
/// (maxAllowedContentLength) must also allow it.
/// </summary>
/// <remarks>
/// It must run before anything reads the request body. The global antiforgery check reads the whole form
/// (it looks for the token field) as an authorization filter with order 1000, and after that the limit can no
/// longer change. So this is an authorization filter with order 900, like ASP.NET Core's own [RequestSizeLimit].
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UploadLimitAttribute(int images = 0, int videos = 0) : Attribute, IFilterFactory, IOrderedFilter
{
    public bool IsReusable => true;

    public int Order => 900;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var storage = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
        var megabytes = (long)images * storage.MaxImageMegabytes + (long)videos * storage.MaxVideoMegabytes + 1;
        return new LimitFilter(megabytes * 1024 * 1024);
    }

    private sealed class LimitFilter(long maxBytes) : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var features = context.HttpContext.Features;
            if (features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } size)
            {
                size.MaxRequestBodySize = maxBytes;
            }

            // The form reader has its own limit for multipart bodies (128 MB by default); keep the two in step.
            if (features.Get<IFormFeature>()?.Form is null)
            {
                features.Set<IFormFeature>(new FormFeature(
                    context.HttpContext.Request, new FormOptions { MultipartBodyLengthLimit = maxBytes }));
            }
        }
    }
}
