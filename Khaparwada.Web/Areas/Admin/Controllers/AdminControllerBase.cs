using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>
/// Base for every admin screen: signed-in Admins and Editors only, plus the admin response headers.
/// Screens for Admins only add [Authorize(Policy = Policies.AdminOnly)].
/// </summary>
[Area("Admin")]
[Authorize(Policy = Policies.Staff)]
[AdminPageHeaders]
public abstract class AdminControllerBase : Controller
{
    public const string StatusMessageKey = "StatusMessage";
    public const string ErrorMessageKey = "ErrorMessage";

    /// <summary>A message shown at the top of the next page ("Saved."), after the redirect.</summary>
    protected void SetStatusMessage(string message) => TempData[StatusMessageKey] = message;

    protected void SetErrorMessage(string message) => TempData[ErrorMessageKey] = message;

    /// <summary>Shows a service's errors next to the form fields they belong to.</summary>
    protected void AddErrors(OperationResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Field, error.Message);
        }
    }
}

/// <summary>
/// Headers for every admin response: never cached (on a shared office computer the Back button must not show
/// admin pages after signing out), never indexed by search engines, never shown inside another site's frame
/// (clickjacking).
/// </summary>
public sealed class AdminPageHeadersAttribute : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        headers.CacheControl = "no-store, no-cache";
        headers.Pragma = "no-cache";
        headers["X-Robots-Tag"] = "noindex, nofollow";
        headers.XFrameOptions = "DENY";
        base.OnResultExecuting(context);
    }
}
