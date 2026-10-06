using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Areas.Admin.Controllers;

[Route("admin/useful-links")]
[Authorize(Policy = Policies.AdminOnly)]
public class UsefulLinksController(IUsefulLinkAdminService links) : AdminControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await links.ListAsync(cancellationToken));

    [HttpGet("new")]
    public IActionResult Create() => View("Edit", new UsefulLinkInputModel());

    [HttpPost("new")]
    public Task<IActionResult> Create(UsefulLinkInputModel input, CancellationToken cancellationToken)
    {
        input.Id = null;
        return SaveAsync(input, "Link added.", cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await links.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}")]
    public Task<IActionResult> Edit(int id, UsefulLinkInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;
        return SaveAsync(input, "Link saved.", cancellationToken);
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await links.GetAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await links.DeleteAsync(id, cancellationToken);
        if (result.Succeeded)
        {
            SetStatusMessage("Link deleted.");
        }
        else
        {
            SetErrorMessage(result.Errors[0].Message);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(UsefulLinkInputModel input, string successMessage, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await links.SaveAsync(input, cancellationToken);
            if (result.Succeeded)
            {
                SetStatusMessage(successMessage);
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View("Edit", input);
    }
}
