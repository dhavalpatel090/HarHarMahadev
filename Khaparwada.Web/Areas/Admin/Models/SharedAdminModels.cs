namespace Khaparwada.Web.Areas.Admin.Models;

/// <summary>
/// A photo field on an admin form (Views/Shared/_ImageField): preview, file input, error message and, when a
/// photo exists, a "remove" tick box. Name and RemoveName are the InputModel's property names.
/// </summary>
public sealed record ImageFieldViewModel(string Name, string RemoveName, string Label, string? CurrentUrl, string? Hint = null);

/// <summary>The "Delete …?" page shared by content screens (Views/Shared/ConfirmDelete).</summary>
public sealed record ConfirmDeleteViewModel(int Id, string Name, string Kind);
