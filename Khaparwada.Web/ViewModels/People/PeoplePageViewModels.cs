using Khaparwada.Web.Infrastructure.Media;
using Khaparwada.Web.Infrastructure.Text;
using Khaparwada.Web.ViewModels.Shared;

namespace Khaparwada.Web.ViewModels.People;

/// <summary>A switched-on people category with its number of visible profiles.</summary>
public sealed record PersonCategoryViewModel(string Slug, string Name, string Icon, int Count);

/// <summary>/people: filter by category, search by name or profession, one page of profiles.</summary>
public sealed class PersonListViewModel
{
    public required IReadOnlyList<PersonCategoryViewModel> Categories { get; init; }
    public PersonCategoryViewModel? Category { get; init; }
    public string? Search { get; init; }
    public required IReadOnlyList<PersonCardViewModel> People { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

/// <summary>/people/{slug}. Only what the person agreed to share: no contact details exist to show.</summary>
public sealed class PersonDetailsViewModel
{
    public required string Slug { get; init; }
    public required string FullName { get; init; }
    public required string Profession { get; init; }
    public required string ShortBio { get; init; }
    public string? Achievement { get; init; }
    public string? Contribution { get; init; }
    public string? PhotoPath { get; init; }
    public required PersonCategoryViewModel Category { get; init; }
    public required IReadOnlyList<PersonCardViewModel> MorePeople { get; init; }

    public string Initials => TextHelpers.Initials(FullName);
    public string? PhotoUrl => MediaUrls.Image(PhotoPath, ImageSize.Medium);
}
