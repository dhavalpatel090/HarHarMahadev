using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Text;

namespace Khaparwada.Web.ViewModels.People;

public sealed class PersonCardViewModel
{
    public required string Slug { get; init; }
    public required string FullName { get; init; }
    public required string Profession { get; init; }
    public required string CategoryName { get; init; }
    public required string CategoryIcon { get; init; }
    public string? PhotoPath { get; init; }

    public string? PhotoUrl => MediaUrls.Image(PhotoPath, ImageSize.Small);
    public string Initials => TextHelpers.Initials(FullName);
}
