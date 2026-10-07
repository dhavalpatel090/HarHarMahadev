using System.Globalization;
using Khaparwada.Web.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Services.Media;

/// <summary>The upload folders on disk, and safe paths inside them.</summary>
public interface IUploadStorage
{
    /// <summary>Folder served at /uploads.</summary>
    string PublicRoot { get; }

    /// <summary>Folder for files awaiting review; never served directly.</summary>
    string PrivateRoot { get; }

    /// <summary>
    /// A new storage key such as "site/2026/10/3f2a9c…". Every upload gets a new random name, so a URL's content
    /// never changes (it can be cached for a year) and a visitor's file name is never used on disk.
    /// </summary>
    string NewKey(string folder);

    /// <summary>Full path of a public file. Throws if the key would point outside the uploads folder.</summary>
    string PublicPath(string relativePath);

    void DeletePublicFile(string relativePath);

    /// <summary>Full path of a private file. Throws if the key would point outside the private folder.</summary>
    string PrivatePath(string relativePath);

    void DeletePrivateFile(string relativePath);
}

public sealed class LocalUploadStorage : IUploadStorage
{
    private readonly TimeProvider _timeProvider;

    public LocalUploadStorage(IOptions<StorageOptions> options, IWebHostEnvironment environment, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        PublicRoot = Resolve(options.Value.UploadsPath, environment.ContentRootPath);
        PrivateRoot = Resolve(options.Value.PrivateUploadsPath, environment.ContentRootPath);

        // The /uploads file server cannot start on a folder that does not exist yet.
        Directory.CreateDirectory(PublicRoot);
        Directory.CreateDirectory(PrivateRoot);
    }

    public string PublicRoot { get; }
    public string PrivateRoot { get; }

    public string NewKey(string folder)
    {
        var now = _timeProvider.GetUtcNow();
        // Invariant culture: some cultures use other calendars, which would change the year in the path.
        return string.Create(CultureInfo.InvariantCulture, $"{folder}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}");
    }

    public string PublicPath(string relativePath) => Combine(PublicRoot, relativePath);

    public void DeletePublicFile(string relativePath)
    {
        var path = PublicPath(relativePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public string PrivatePath(string relativePath) => Combine(PrivateRoot, relativePath);

    public void DeletePrivateFile(string relativePath)
    {
        var path = PrivatePath(relativePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string Combine(string root, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));

        // Keys come from the database; refuse anything that would escape the folder, such as "../".
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Upload path '{relativePath}' is outside the uploads folder.");
        }

        return fullPath;
    }

    private static string Resolve(string path, string contentRoot) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(contentRoot, path));
}
