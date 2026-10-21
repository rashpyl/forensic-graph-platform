namespace ForensicGraph.Application.Security;

/// <summary>
/// The three application roles. Every user holds exactly one of them.
/// <list type="bullet">
///   <item><see cref="Admin"/> — manages user accounts and reads the audit log; can also edit data.</item>
///   <item><see cref="Analyst"/> — creates, edits and deletes investigation data and sees sensitive fields.</item>
///   <item><see cref="Viewer"/> — read-only access; sensitive person fields are masked.</item>
/// </list>
/// All roles see the same shared graph: access is differentiated by what a user
/// may do, not by which records they may see.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Analyst = "Analyst";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Analyst, Viewer };

    public static bool IsValid(string? role) =>
        role is not null && All.Contains(role, StringComparer.Ordinal);
}

/// <summary>Names of the authorization policies applied to API endpoints.</summary>
public static class Policies
{
    /// <summary>Create, update and delete investigation data. Roles: Admin, Analyst.</summary>
    public const string CanEdit = "CanEdit";

    /// <summary>Manage users and read the audit log. Role: Admin.</summary>
    public const string AdminOnly = "AdminOnly";
}
