using Khaparwada.Web.Infrastructure.Routing;
using Khaparwada.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace Khaparwada.Web.Controllers;

/// <summary>
/// Placeholder pages for the sections built in later phases, so every menu link works and the final URLs
/// exist from now on. When a phase builds the real page, it deletes the action here and puts the same route
/// name on the new action (a duplicate route name fails at startup, which makes a leftover impossible to miss).
/// </summary>
public class ComingSoonController : Controller
{
    [HttpGet("gallery", Name = RouteNames.Gallery)]
    public IActionResult Gallery() =>
        Placeholder(RouteNames.Gallery, "Gallery", "Photos of festivals, people, places and the old village.", "images");

    [HttpGet("gallery/{slug}", Name = RouteNames.AlbumDetails)]
    public IActionResult AlbumDetails(string slug) => Gallery();

    [HttpGet("notices", Name = RouteNames.Notices)]
    public IActionResult Notices() =>
        Placeholder(RouteNames.Notices, "Notice board", "Announcements, government schemes, water and electricity updates, meetings and emergencies.", "bullhorn");

    [HttpGet("notices/{slug}", Name = RouteNames.NoticeDetails)]
    public IActionResult NoticeDetails(string slug) => Notices();

    [HttpGet("memories", Name = RouteNames.Memories)]
    public IActionResult Memories() =>
        Placeholder(RouteNames.Memories, "Village memories", "Old photos, stories and memories shared by residents.", "camera");

    [HttpGet("memories/share", Name = RouteNames.ShareMemory)]
    public IActionResult ShareMemory() => Memories();

    [HttpGet("memories/{slug}", Name = RouteNames.MemoryDetails)]
    public IActionResult MemoryDetails(string slug) => Memories();

    [HttpGet("contact", Name = RouteNames.Contact)]
    public IActionResult Contact() =>
        Placeholder(RouteNames.Contact, "Contact", "Location, contact details and useful links.", "envelope");

    private ViewResult Placeholder(string section, string title, string lead, string icon)
    {
        ViewData[SiteNavigation.SectionKey] = section;
        return View("ComingSoon", new ComingSoonViewModel(title, lead, icon));
    }
}
