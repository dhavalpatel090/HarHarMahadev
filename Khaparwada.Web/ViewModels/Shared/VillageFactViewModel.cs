namespace Khaparwada.Web.ViewModels.Shared;

/// <summary>One "label: value" fact about the village, e.g. "District: Navsari".</summary>
public sealed record VillageFactViewModel(string Label, string Value);
