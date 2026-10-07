using Khaparwada.Web.ViewModels.Shared;
using Microsoft.Extensions.Localization;

namespace Khaparwada.Web.Services.Site;

/// <summary>The "label: value" facts about the village, built the same way for Home and About. Empty values are left out.</summary>
public static class VillageFacts
{
    /// <summary>Where the village is: taluka, district, state, PIN code.</summary>
    public static List<VillageFactViewModel> Location(SiteInfo site, IStringLocalizer text)
    {
        var facts = new List<VillageFactViewModel>();
        Add(facts, text["Taluka"], site.Taluka);
        Add(facts, text["District"], site.District);
        Add(facts, text["State"], site.State);
        Add(facts, text["PIN code"], site.PinCode);
        return facts;
    }

    /// <summary>The figures typed in by the admin: population, families, area.</summary>
    public static List<VillageFactViewModel> Figures(SiteInfo site, IStringLocalizer text)
    {
        var facts = new List<VillageFactViewModel>();
        Add(facts, text["Population"], site.Population?.ToString("N0"));
        Add(facts, text["Families"], site.Households?.ToString("N0"));
        Add(facts, text["Area"], site.AreaSqKm is { } area ? text["{0} km²", area.ToString("0.##")].Value : null);
        return facts;
    }

    private static void Add(List<VillageFactViewModel> facts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            facts.Add(new VillageFactViewModel(label, value));
        }
    }
}
