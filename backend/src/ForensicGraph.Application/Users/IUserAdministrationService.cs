namespace ForensicGraph.Application.Users;

/// <summary>
/// Account management performed by administrators. Accounts are never deleted —
/// they are deactivated — so that audit entries keep pointing at a real user.
/// Implemented in Infrastructure on top of ASP.NET Core Identity.
/// </summary>
public interface IUserAdministrationService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    /// <summary>Changes a user's role. Throws <c>ConflictException</c> if it would leave no active admin.</summary>
    Task<UserDto> ChangeRoleAsync(Guid userId, string role, Guid actingUserId, CancellationToken cancellationToken);

    /// <summary>Deactivates an account. Admins cannot deactivate themselves.</summary>
    Task<UserDto> DeactivateAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken);

    /// <summary>Re-activates an account and clears any failed-login lockout.</summary>
    Task<UserDto> ActivateAsync(Guid userId, CancellationToken cancellationToken);

    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);

    Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<CurrentUserDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);
}
