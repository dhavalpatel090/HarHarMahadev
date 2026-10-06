using Khaparwada.Web.Areas.Admin.Models;
using Khaparwada.Web.Infrastructure.Security;
using Khaparwada.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Khaparwada.Web.Areas.Admin.Controllers;

/// <summary>
/// Sign in, sign out and change password. There is deliberately no "register" or "forgot password" page:
/// admins create accounts for each other (Phase 11), and no email sending is set up.
/// </summary>
[Area("Admin")]
[Route("admin")]
[AdminPageHeaders]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginInputModel { ReturnUrl = returnUrl });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.LoginRateLimit)]
    public async Task<IActionResult> Login(LoginInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await userManager.FindByEmailAsync(input.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "The email or password is not correct.");
            return View(input);
        }

        // The password is checked first (wrong attempts count towards the lockout). Only someone who knows it
        // learns that the account is switched off; anyone else could otherwise use that message to find out
        // which email addresses have accounts.
        var result = await signInManager.CheckPasswordSignInAsync(user, input.Password, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account has been switched off. Please contact the site administrator.");
                return View(input);
            }

            // Not persistent: the sign-in ends when the browser closes, which is safer on shared computers.
            await signInManager.SignInAsync(user, isPersistent: false);
            user.LastLoginAt = timeProvider.GetUtcNow();
            await userManager.UpdateAsync(user);
            logger.LogInformation("Admin user {UserId} signed in.", user.Id);
            return RedirectToLocal(input.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Admin user {UserId} is locked out after repeated failed sign-ins.", user.Id);
            ModelState.AddModelError(string.Empty, "Too many wrong passwords. This account is locked for 15 minutes.");
            return View(input);
        }

        ModelState.AddModelError(string.Empty, "The email or password is not correct.");
        return View(input);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("access-denied")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpGet("change-password")]
    [Authorize(Policy = Policies.Staff)]
    public IActionResult ChangePassword() => View(new ChangePasswordInputModel());

    [HttpPost("change-password")]
    [Authorize(Policy = Policies.Staff)]
    public async Task<IActionResult> ChangePassword(ChangePasswordInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var field = error.Code == "PasswordMismatch" ? nameof(input.CurrentPassword) : nameof(input.NewPassword);
                ModelState.AddModelError(field, error.Code == "PasswordMismatch" ? "The current password is not correct." : error.Description);
            }

            return View(input);
        }

        // Changing the password changes the security stamp; refresh this sign-in so it stays valid.
        await signInManager.RefreshSignInAsync(user);
        TempData[AdminControllerBase.StatusMessageKey] = "Your password has been changed.";
        return RedirectToAction("Index", "Dashboard");
    }

    /// <summary>Only follows addresses on this site; any other address could send the user to a fake page.</summary>
    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Dashboard");
}
