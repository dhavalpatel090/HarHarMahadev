namespace Khaparwada.Web.Services.Media;

/// <summary>
/// One photo field of an admin form, from upload to clean-up, so no screen can leave orphaned files behind:
/// <list type="number">
/// <item><see cref="PrepareAsync"/> saves a newly chosen photo (or returns the upload error),</item>
/// <item><see cref="Apply"/> gives the path to store: the new photo, none (removed) or the current one,</item>
/// <item>after the database save, <see cref="SaveSucceeded"/> deletes the replaced photo's files, or
/// <see cref="SaveFailed"/> deletes the new upload.</item>
/// </list>
/// Replaced files are deleted only after the save, so a failed save never leaves a record pointing at
/// files that no longer exist.
/// </summary>
public sealed class ImageEdit
{
    private readonly IImageUploadService _images;
    private readonly string? _uploaded;
    private readonly bool _remove;
    private string? _replaced;

    private ImageEdit(IImageUploadService images, string? uploaded, bool remove, string? error)
    {
        _images = images;
        _uploaded = uploaded;
        _remove = remove;
        Error = error;
    }

    /// <summary>The message to show next to the upload field, if the chosen file was refused.</summary>
    public string? Error { get; }

    public bool Failed => Error is not null;

    /// <summary>True when the form brings a new photo or asks to remove the current one.</summary>
    public bool ChangesImage => _uploaded is not null || _remove;

    public static async Task<ImageEdit> PrepareAsync(
        IImageUploadService images, IFormFile? file, bool remove, string folder, CancellationToken cancellationToken)
    {
        if (file is not { Length: > 0 })
        {
            return new ImageEdit(images, null, remove, null);
        }

        var result = await images.SaveAsync(file, folder, cancellationToken);
        return result.Succeeded
            ? new ImageEdit(images, result.Image!.Path, remove, null)
            : new ImageEdit(images, null, remove, result.Error);
    }

    /// <summary>The path <see cref="Apply"/> will return, without recording anything: for checks before saving.</summary>
    public string? Peek(string? currentPath) => ChangesImage ? _uploaded : currentPath;

    /// <summary>A new photo wins over "remove"; with neither, the current path is kept.</summary>
    public string? Apply(string? currentPath)
    {
        if (!ChangesImage)
        {
            return currentPath;
        }

        _replaced = currentPath;
        return _uploaded;
    }

    public void SaveSucceeded() => _images.Delete(_replaced);

    public void SaveFailed() => _images.Delete(_uploaded);
}
