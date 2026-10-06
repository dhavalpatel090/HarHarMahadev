using Khaparwada.Web.Models.Common;

namespace Khaparwada.Web.Models.Entities;

/// <summary>
/// A photo sent with a memory. While the memory is unpublished the file lives in private storage,
/// which has no public URL; it moves to public storage when the memory is published.
/// </summary>
public class MemoryImage : BaseEntity
{
    public int MemorySubmissionId { get; set; }
    public MemorySubmission MemorySubmission { get; set; } = null!;

    public string ImagePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
}
