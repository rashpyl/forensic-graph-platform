using ForensicGraph.Application.Security;

namespace ForensicGraph.Tests.TestDoubles;

public sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(string role, Guid? userId = null, string? userName = null)
    {
        Role = role;
        UserId = userId ?? Guid.NewGuid();
        UserName = userName ?? $"{role.ToLowerInvariant()}@test.local";
    }

    public string Role { get; }

    public Guid? UserId { get; }

    public string? UserName { get; }

    public bool IsInRole(string role) => string.Equals(role, Role, StringComparison.Ordinal);

    public static FakeCurrentUser Analyst() => new(Roles.Analyst);

    public static FakeCurrentUser Viewer() => new(Roles.Viewer);
}
