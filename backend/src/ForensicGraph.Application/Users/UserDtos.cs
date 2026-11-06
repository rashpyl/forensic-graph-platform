namespace ForensicGraph.Application.Users;

/// <summary>A user account as seen by an administrator.</summary>
public sealed record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;

    /// <summary>True when the account is deactivated by an admin or temporarily locked after failed logins.</summary>
    public bool IsLocked { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>The signed-in user's own profile, returned by <c>GET /api/auth/me</c>.</summary>
public sealed record CurrentUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed record CreateUserRequest
{
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;

    /// <summary>Initial password chosen by the admin and handed to the user out of band.</summary>
    public string Password { get; init; } = string.Empty;
}

public sealed record ChangeRoleRequest
{
    public string Role { get; init; } = string.Empty;
}

public sealed record ResetPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}

public sealed record ChangeOwnPasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
