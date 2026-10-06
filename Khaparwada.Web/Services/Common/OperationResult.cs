namespace Khaparwada.Web.Services.Common;

/// <summary>
/// Outcome of an admin save: success, or errors keyed by form field name, so the controller can show each
/// message next to the field it belongs to.
/// </summary>
public sealed class OperationResult
{
    private readonly List<OperationError> _errors = [];

    public bool Succeeded => _errors.Count == 0;
    public IReadOnlyList<OperationError> Errors => _errors;

    public static OperationResult Success() => new();

    public static OperationResult Failure(string field, string message) => new OperationResult().AddError(field, message);

    public OperationResult AddError(string field, string message)
    {
        _errors.Add(new OperationError(field, message));
        return this;
    }
}

/// <summary>An error for one form field; an empty field name means "the form as a whole".</summary>
public sealed record OperationError(string Field, string Message);
