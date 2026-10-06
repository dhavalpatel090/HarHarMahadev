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
}
