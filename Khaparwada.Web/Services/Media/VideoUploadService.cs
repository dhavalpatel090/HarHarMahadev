using Khaparwada.Web.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Services.Media;

public sealed record VideoUploadResult(string? Path, string? Error)
{
    public bool Succeeded => Path is not null;

    public static VideoUploadResult Failed(string error) => new(null, error);
}

public interface IVideoUploadService
{
    /// <summary>
    /// Saves a short MP4 for the Home page background. Checked by extension, size and content: every MP4 file
    /// has "ftyp" at bytes 4–7, so a renamed file of another type is refused.
    /// </summary>
    Task<VideoUploadResult> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    void Delete(string? videoPath);
}

public sealed class VideoUploadService(IUploadStorage storage, IOptions<StorageOptions> options) : IVideoUploadService
{
    public async Task<VideoUploadResult> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var maxMegabytes = options.Value.MaxVideoMegabytes;
        if (file.Length == 0)
        {
            return VideoUploadResult.Failed("The chosen file is empty.");
        }

        if (file.Length > maxMegabytes * 1024L * 1024L)
        {
            return VideoUploadResult.Failed($"The video is larger than {maxMegabytes} MB. Please choose a shorter one.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return VideoUploadResult.Failed("Please choose an MP4 video.");
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[8];
        if (await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken) < header.Length
            || header[4] != (byte)'f' || header[5] != (byte)'t' || header[6] != (byte)'y' || header[7] != (byte)'p')
        {
            return VideoUploadResult.Failed("This file is not an MP4 video.");
        }

        stream.Position = 0;
        var relativePath = storage.NewKey(folder) + ".mp4";
        var fullPath = storage.PublicPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var target = File.Create(fullPath))
        {
            await stream.CopyToAsync(target, cancellationToken);
        }

        return new VideoUploadResult(relativePath, null);
    }

    public void Delete(string? videoPath)
    {
        if (!string.IsNullOrWhiteSpace(videoPath))
        {
            storage.DeletePublicFile(videoPath);
        }
    }
}
