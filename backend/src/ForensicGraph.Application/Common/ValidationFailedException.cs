namespace ForensicGraph.Application.Common;

/// <summary>
/// Thrown when a request is well-formed but violates a rule checked outside
/// FluentValidation (for example the password policy enforced by Identity).
/// Mapped to HTTP 400 by the global exception handler.
/// </summary>
public sealed class ValidationFailedException : Exception
{
    public ValidationFailedException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
