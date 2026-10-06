namespace Khaparwada.Web.Models.Common;

/// <summary>
/// What place, person and gallery categories have in common, so the admin code for names, web addresses,
/// ordering and switching on/off is written once.
/// </summary>
public interface ICategory
{
    int Id { get; }
    string Name { get; set; }
    string? NameGu { get; set; }
    string Slug { get; set; }
    int SortOrder { get; set; }
    bool IsActive { get; set; }
}
