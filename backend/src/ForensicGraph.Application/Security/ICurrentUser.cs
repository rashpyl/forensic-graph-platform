namespace ForensicGraph.Application.Security;

/// <summary>
/// The authenticated user on whose behalf the current request runs.
/// Implemented in the API layer from the HTTP context; application services
/// use it to decide what a caller may see (sensitive-field masking).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Id of the signed-in user, or <c>null</c> for anonymous / background work.</summary>
    Guid? UserId { get; }

    string? UserName { get; }

    bool IsInRole(string role);
}

public static class CurrentUserExtensions
{
    /// <summary>
    /// Whether the user may read sensitive personal identifiers (passport numbers, phone).
    /// Viewers get a masked value instead.
    /// </summary>
    public static bool CanSeeSensitiveData(this ICurrentUser user) =>
        user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Analyst);
}
