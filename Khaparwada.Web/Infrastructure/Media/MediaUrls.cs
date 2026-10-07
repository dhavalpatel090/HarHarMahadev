namespace Khaparwada.Web.Infrastructure.Media;

public enum ImageSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// URLs of uploaded images. The database stores one base key per image (e.g. "places/2026/10/3f2a…") and
/// every upload is saved in three sizes next to each other (see Services/Media/ImageUploadService).
/// </summary>
public static class MediaUrls
{
    public const string RequestPath = "/uploads";

    /// <summary>Default artwork used wherever no photo has been uploaded yet.</summary>
    public const string VillageIllustration = "/images/illustrations/village-dusk.svg";

    public static string? Image(string? imagePath, ImageSize size = ImageSize.Medium)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var suffix = size switch
        {
            ImageSize.Small => "sm",
            ImageSize.Large => "lg",
            _ => "md"
        };
        return $"{RequestPath}/{imagePath}-{suffix}.webp";
    }

    /// <summary>
    /// The three saved sizes for an img srcset, so phones download the 480 px file and large screens the
    /// 1920 px one. Widths are each size's longest side (exact for landscape photos).
    /// </summary>
    public static string? SrcSet(string? imagePath) =>
        string.IsNullOrWhiteSpace(imagePath)
            ? null
            : $"{Image(imagePath, ImageSize.Small)} 480w, {Image(imagePath, ImageSize.Medium)} 960w, {Image(imagePath, ImageSize.Large)} 1920w";
}
