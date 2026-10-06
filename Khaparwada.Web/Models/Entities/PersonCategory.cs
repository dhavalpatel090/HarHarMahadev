using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>Category of community profile (Teachers, Doctors, Farmers, …).</summary>
public class PersonCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameGu { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Person> People { get; set; } = new List<Person>();
}
