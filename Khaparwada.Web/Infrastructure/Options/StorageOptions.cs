using System.ComponentModel.DataAnnotations;

namespace Khaparwada.Web.Infrastructure.Options;

/// <summary>
/// Settings from the "Storage" section of appsettings.json: where uploads are kept and how big they may be.
/// Relative paths are inside the app's folder, which is fine for development. In production use absolute
/// folders outside the deployment folder, so publishing a new version can never delete villagers' photos.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Approved files, served to visitors at /uploads.</summary>
    [Required]
    public string UploadsPath { get; set; } = "App_Data/uploads";

    /// <summary>
    /// Files that are never served directly: residents' submissions waiting for review (Phase 10) and notice
    /// attachments (sent by /notices/{slug}/attachment only while the notice is shown).
    /// </summary>
    [Required]
    public string PrivateUploadsPath { get; set; } = "App_Data/private-uploads";

    [Range(1, 50)]
    public int MaxImageMegabytes { get; set; } = 10;

    /// <summary>
    /// Largest image accepted, in megapixels. Blocks "decompression bomb" files that are small on disk but
    /// need gigabytes of memory to open.
    /// </summary>
    [Range(1, 100)]
    public int MaxImageMegapixels { get; set; } = 40;

    [Range(1, 100)]
    public int MaxVideoMegabytes { get; set; } = 25;

    /// <summary>Largest PDF attachment of a notice (e.g. a scheme's form).</summary>
    [Range(1, 50)]
    public int MaxDocumentMegabytes { get; set; } = 10;
}
