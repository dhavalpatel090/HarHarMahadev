using Khaparwada.Web.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Khaparwada.Web.Services.Media;

/// <summary>A stored image: the base key saved in the database, plus the size of its largest version.</summary>
public sealed record StoredImage(string Path, int Width, int Height);

/// <summary>Either a stored image or a message to show next to the upload field.</summary>
public sealed record ImageUploadResult(StoredImage? Image, string? Error)
{
    public bool Succeeded => Image is not null;

    public static ImageUploadResult Failed(string error) => new(null, error);
}

public interface IImageUploadService
{
    /// <summary>
    /// Checks an uploaded photo and saves a cleaned copy: JPEG, PNG or WebP only (checked by extension and by
    /// content), within the size limits; turned upright, stripped of hidden metadata such as GPS location,
    /// converted to standard colours and saved as WebP in three sizes ("-sm", "-md", "-lg").
    /// </summary>
    Task<ImageUploadResult> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);

    /// <summary>Deletes every size of a stored image. Does nothing for null.</summary>
    void Delete(string? imagePath);
}

/// <summary>
/// Built on SkiaSharp (MIT licence, maintained by Microsoft). Its decoders are libjpeg-turbo, libpng and libwebp,
/// the same ones Chrome uses, which are fuzz-tested continuously; that matters because residents' photos
/// (Phase 10) come from people we cannot vet. Only the three decoders below are ever reached.
/// </summary>
public sealed class ImageUploadService(
    IUploadStorage storage,
    IOptions<StorageOptions> options,
    ILogger<ImageUploadService> logger) : IImageUploadService
{
    private const string WrongTypeMessage = "Please choose a JPG, PNG or WebP photo.";
    private const int WebpQuality = 80;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly SKEncodedImageFormat[] AllowedFormats =
        [SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Png, SKEncodedImageFormat.Webp];

    // Longest side of each saved size; the suffixes match MediaUrls.
    private static readonly (string Suffix, int MaxSide)[] Sizes = [("sm", 480), ("md", 960), ("lg", 1920)];

    private static readonly SKColorSpace Srgb = SKColorSpace.CreateSrgb();

    public async Task<ImageUploadResult> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        var limits = options.Value;
        if (file.Length == 0)
        {
            return ImageUploadResult.Failed("The chosen file is empty.");
        }

        if (file.Length > limits.MaxImageMegabytes * 1024L * 1024L)
        {
            return ImageUploadResult.Failed($"The photo is larger than {limits.MaxImageMegabytes} MB. Please choose a smaller one.");
        }

        // The name can lie, so it is only the first check; the content is checked below.
        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
        {
            return ImageUploadResult.Failed(WrongTypeMessage);
        }

        using var buffer = new MemoryStream((int)file.Length);
        await using (var upload = file.OpenReadStream())
        {
            await upload.CopyToAsync(buffer, cancellationToken);
        }

        using var data = SKData.CreateCopy(buffer.GetBuffer(), (ulong)buffer.Length);
        using var codec = SKCodec.Create(data); // reads only the header: format, size, orientation
        if (codec is null)
        {
            return ImageUploadResult.Failed("This file is not a photo that can be read. " + WrongTypeMessage);
        }

        // The real format comes from the file's first bytes, whatever its name says.
        if (!AllowedFormats.Contains(codec.EncodedFormat))
        {
            return ImageUploadResult.Failed(WrongTypeMessage);
        }

        // Checked before any pixels are decoded: a "decompression bomb" is small on disk but huge in memory.
        if ((long)codec.Info.Width * codec.Info.Height > limits.MaxImageMegapixels * 1_000_000L)
        {
            return ImageUploadResult.Failed(
                $"The photo is too large ({codec.Info.Width} × {codec.Info.Height} pixels). The limit is {limits.MaxImageMegapixels} megapixels.");
        }

        using var decoded = Decode(codec, Sizes[^1].MaxSide);
        if (decoded is null)
        {
            return ImageUploadResult.Failed("This photo is damaged and cannot be opened. " + WrongTypeMessage);
        }

        decoded.SetImmutable(); // lets the image below share the pixels instead of copying them
        using var source = SKImage.FromBitmap(decoded);

        var key = storage.NewKey(folder);
        var written = new List<string>();
        var (width, height) = (0, 0);
        try
        {
            foreach (var (suffix, maxSide) in Sizes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Re-encoding writes only pixels, so camera details and GPS location never reach the saved file.
                using var resized = Render(source, codec.EncodedOrigin, maxSide);
                using var pixels = resized.PeekPixels();
                using var webp = pixels.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossy, WebpQuality))
                    ?? throw new InvalidOperationException("The WebP encoder returned no data.");

                var relativePath = $"{key}-{suffix}.webp";
                var fullPath = storage.PublicPath(relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllBytesAsync(fullPath, webp.ToArray(), cancellationToken);
                written.Add(relativePath);
                (width, height) = (resized.Width, resized.Height);
            }
        }
        catch
        {
            // Never leave half a set of sizes behind.
            written.ForEach(storage.DeletePublicFile);
            throw;
        }

        logger.LogInformation("Saved image {Key} ({Width}×{Height}) from a {Bytes}-byte upload.", key, width, height, file.Length);
        return new ImageUploadResult(new StoredImage(key, width, height), null);
    }

    public void Delete(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return;
        }

        foreach (var (suffix, _) in Sizes)
        {
            storage.DeletePublicFile($"{imagePath}-{suffix}.webp");
        }
    }

    /// <summary>
    /// Decodes at the smallest size the format allows that is still at least <paramref name="largestSide"/> on
    /// its longest side (JPEG shrinks while decoding, in eighths): a 48-megapixel phone photo then needs about
    /// 16 MB of memory instead of 190 MB. Colours are converted from the photo's own profile (for example the
    /// Display P3 of newer phones) to standard sRGB. Returns null for a damaged or cut-off file.
    /// </summary>
    private static SKBitmap? Decode(SKCodec codec, int largestSide)
    {
        var info = codec.Info;

        // +1/16 because JPEG rounds to the nearest eighth, which could otherwise land just below the size needed.
        var scale = Math.Min(1f, largestSide / (float)Math.Max(info.Width, info.Height) + 0.0625f);
        var size = codec.GetScaledDimensions(scale);
        var alphaType = info.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;
        var target = new SKImageInfo(size.Width, size.Height, SKImageInfo.PlatformColorType, alphaType, Srgb);

        var bitmap = new SKBitmap(target);
        if (codec.GetPixels(target, bitmap.GetPixels()) == SKCodecResult.Success)
        {
            return bitmap;
        }

        bitmap.Dispose();
        return null;
    }

    /// <summary>
    /// Draws the photo the right way up, shrunk to fit a <paramref name="maxSide"/> square (never enlarged).
    /// The result has no colour profile attached: its pixels are already sRGB, which is what browsers assume
    /// when there is none, and it keeps a profile out of every saved file.
    /// </summary>
    private static SKBitmap Render(SKImage source, SKEncodedOrigin origin, int maxSide)
    {
        // Orientations 5–8 are stored on their side: the upright photo swaps width and height.
        var sideways = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var (uprightWidth, uprightHeight) = sideways ? (source.Height, source.Width) : (source.Width, source.Height);

        var scale = Math.Min(1.0, maxSide / (double)Math.Max(uprightWidth, uprightHeight));
        var width = Math.Max(1, (int)Math.Round(uprightWidth * scale));
        var height = Math.Max(1, (int)Math.Round(uprightHeight * scale));

        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, source.AlphaType));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(SKMatrix.Concat(
            SKMatrix.CreateScale(width / (float)uprightWidth, height / (float)uprightHeight),
            OrientationMatrix(origin, source.Width, source.Height)));

        // Large reductions read from pre-shrunk copies (mipmaps), which avoids jagged edges and moiré patterns;
        // small ones use a sharper bicubic filter.
        var sampling = scale < 0.5
            ? new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
            : new SKSamplingOptions(SKCubicResampler.Mitchell);
        canvas.DrawImage(source, 0, 0, sampling);
        return bitmap;
    }

    /// <summary>
    /// Phones store "turn me" in the EXIF orientation tag instead of turning the pixels. This maps the stored
    /// pixels (<paramref name="width"/> × <paramref name="height"/>) onto the upright photo, one case per tag value.
    /// </summary>
    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, int width, int height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),       // 2: mirrored
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1), // 3: upside down
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),    // 4: mirrored upside down
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),             // 5: mirrored and on its side
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),      // 6: turn ¼ clockwise (most portrait phone photos)
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1), // 7: mirrored and on its side
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),     // 8: turn ¼ anticlockwise
        _ => SKMatrix.Identity                                                          // 1: already upright
    };
}
