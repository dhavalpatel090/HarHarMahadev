using Khaparwada.Web.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Khaparwada.Web.Services.Media;

/// <summary>A saved document: its private storage key, the name to show and its size.</summary>
public sealed record StoredDocument(string Path, string FileName, long Bytes);

public sealed record DocumentUploadResult(StoredDocument? Document, string? Error)
{
    public bool Succeeded => Document is not null;

    public static DocumentUploadResult Failed(string error) => new(null, error);
}

public interface IDocumentUploadService
{
    /// <summary>
    /// Saves a PDF in the private folder (never served by /uploads). Checked by extension, size and content:
    /// every PDF starts with "%PDF-", so a renamed file of another type is refused. Only PDF: Word and Excel
    /// files can carry macros, and every phone opens a PDF.
    /// </summary>
    Task<DocumentUploadResult> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    void Delete(string? documentPath);
}

public sealed class DocumentUploadService(IUploadStorage storage, IOptions<StorageOptions> options) : IDocumentUploadService
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public async Task<DocumentUploadResult> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var maxMegabytes = options.Value.MaxDocumentMegabytes;
        if (file.Length == 0)
        {
            return DocumentUploadResult.Failed("The chosen file is empty.");
        }

        if (file.Length > maxMegabytes * 1024L * 1024L)
        {
            return DocumentUploadResult.Failed($"The file is larger than {maxMegabytes} MB. Please choose a smaller one.");
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentUploadResult.Failed("Please choose a PDF file.");
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[PdfSignature.Length];
        if (await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken) < header.Length
            || !header.AsSpan().SequenceEqual(PdfSignature))
        {
            return DocumentUploadResult.Failed("This file is not a PDF document.");
        }

        stream.Position = 0;
        var relativePath = storage.NewKey(folder) + ".pdf";
        var fullPath = storage.PrivatePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var target = File.Create(fullPath))
        {
            await stream.CopyToAsync(target, cancellationToken);
        }

        return new DocumentUploadResult(new StoredDocument(relativePath, CleanFileName(file.FileName), file.Length), null);
    }

    public void Delete(string? documentPath)
    {
        if (!string.IsNullOrWhiteSpace(documentPath))
        {
            storage.DeletePrivateFile(documentPath);
        }
    }

    /// <summary>
    /// The name shown on the download link and offered when saving: the visitor's own file name without any
    /// folder part, control characters or quotes, at most 255 characters, always ending in ".pdf".
    /// </summary>
    public static string CleanFileName(string original)
    {
        var name = Path.GetFileNameWithoutExtension(original.Replace('\\', '/').Split('/').Last());
        name = new string(name.Where(c => !char.IsControl(c) && c != '"').ToArray()).Trim();
        if (name.Length == 0)
        {
            name = "document";
        }

        return (name.Length > 200 ? name[..200] : name) + ".pdf";
    }
}
