using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>The three category lists: /admin/categories/places, /people and /gallery.</summary>
[Route("admin/categories")]
[Authorize(Policy = Policies.AdminOnly)]
public class CategoriesController(ICategoryAdminService categories) : AdminControllerBase
{
    private const string Kind = "{kind:regex(^(places|people|gallery)$)}";

    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(List), new { kind = CategoryKind.Places });

    [HttpGet(Kind)]
    public async Task<IActionResult> List(CategoryKind kind, CancellationToken cancellationToken) =>
        View(new CategoryListViewModel { Kind = kind, Items = await categories.ListAsync(kind, cancellationToken) });

    [HttpGet(Kind + "/new")]
    public IActionResult Create(CategoryKind kind) => View("Edit", new CategoryInputModel { Kind = kind });

    [HttpPost(Kind + "/new")]
    public Task<IActionResult> Create(CategoryKind kind, CategoryInputModel input, CancellationToken cancellationToken)
    {
        input.Kind = kind;
        input.Id = null;
        return SaveAsync(input, "Category added.", cancellationToken);
    }

    [HttpGet(Kind + "/{id:int}")]
    public async Task<IActionResult> Edit(CategoryKind kind, int id, CancellationToken cancellationToken)
    {
        var model = await categories.GetAsync(kind, id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost(Kind + "/{id:int}")]
    public Task<IActionResult> Edit(CategoryKind kind, int id, CategoryInputModel input, CancellationToken cancellationToken)
    {
        input.Kind = kind;
        input.Id = id;
        return SaveAsync(input, "Category saved.", cancellationToken);
    }

    [HttpGet(Kind + "/{id:int}/delete")]
    public async Task<IActionResult> Delete(CategoryKind kind, int id, CancellationToken cancellationToken)
    {
        var model = await categories.GetDeleteInfoAsync(kind, id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost(Kind + "/{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(CategoryKind kind, int id, CancellationToken cancellationToken)
    {
        var result = await categories.DeleteAsync(kind, id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Category deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(List), new { kind });
    }

    private async Task<IActionResult> SaveAsync(CategoryInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await categories.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage(successMessage);
                return RedirectToAction(nameof(List), new { kind = input.Kind });
            }

            AddErrors(result);
        }

        return View("Edit", input);
    }
}
