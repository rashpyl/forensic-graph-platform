using Microsoft.AspNetCore.Identity;

namespace ForensicGraph.Infrastructure.Identity;

/// <summary>
/// Application user account, stored by ASP.NET Core Identity in the <c>users</c> table.
/// The e-mail address doubles as the login name. Passwords are never stored —
/// Identity keeps a salted PBKDF2 hash (<see cref="IdentityUser{TKey}.PasswordHash"/>).
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Set when an administrator deactivates the account. Distinct from the
    /// temporary lockout Identity applies after repeated failed logins.
    /// </summary>
    public bool IsDeactivated { get; set; }
}
