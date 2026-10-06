namespace Khaparwada.Web.Models.Common;

/// <summary>
/// Maximum text lengths, shared by the database configuration and (from Phase 3) the form
/// validation. One source of truth stops the two drifting apart, which would otherwise show up
/// as SQL "String or binary data would be truncated" errors.
/// Long free text (descriptions, stories) is nvarchar(max) and has no constant here.
/// </summary>
public static class FieldLengths
{
    public const int Name = 150;            // places, people, categories, site name
    public const int Title = 200;           // events, notices, sections, albums
    public const int Slug = 200;            // URL part, e.g. "khaparwada-temple"
    public const int Label = 100;           // year labels, taluka, profession
    public const int Summary = 500;         // card text, map pop-ups, meta descriptions
    public const int Intro = 1000;          // short paragraphs: intro, bio, achievement
    public const int Caption = 300;
    public const int Address = 300;
    public const int Timings = 300;
    public const int Url = 500;
    public const int Email = 256;
    public const int Phone = 20;
    public const int PinCode = 6;
    public const int FilePath = 300;        // storage key of an uploaded file
    public const int FileName = 255;        // original name of an uploaded file
    public const int IconClass = 60;        // Font Awesome classes
    public const int HexColor = 7;          // "#RRGGBB"
    public const int ContactMessage = 4000;
    public const int ReviewNote = 1000;
}
