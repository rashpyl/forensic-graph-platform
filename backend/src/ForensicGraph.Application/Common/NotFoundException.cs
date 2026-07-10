namespace ForensicGraph.Application.Common;

/// <summary>
/// Thrown by application services when a lookup by id yields no result.
/// The API's ProblemDetails middleware maps this to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}
