namespace ForensicGraph.Application.Common;

/// <summary>
/// Thrown by application services when an operation would violate a uniqueness
/// or state invariant — e.g. assigning the same person to the same event under
/// the same role twice, or linking an event to itself. The API's ProblemDetails
/// middleware maps this to HTTP 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
