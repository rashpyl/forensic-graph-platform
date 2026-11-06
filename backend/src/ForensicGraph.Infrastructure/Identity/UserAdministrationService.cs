using ForensicGraph.Application.Common;
using ForensicGraph.Application.Security;
using ForensicGraph.Application.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.Identity;

/// <summary>
/// <see cref="IUserAdministrationService"/> on top of ASP.NET Core Identity's
/// <see cref="UserManager{TUser}"/>. Every user holds exactly one role.
///
/// Deactivation sets <see cref="AppUser.IsDeactivated"/>, locks the account
/// indefinitely and rotates the security stamp, which invalidates the user's
/// existing sessions at the next cookie validation.
/// </summary>
public sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly UserManager<AppUser> _users;
    private readonly TimeProvider _timeProvider;

    public UserAdministrationService(UserManager<AppUser> users, TimeProvider timeProvider)
    {
        _users = users;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var all = await _users.Users
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var result = new List<UserDto>(all.Count);
        foreach (var user in all)
        {
            result.Add(await ToDtoAsync(user));
        }

        return result;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        EnsureValidRole(request.Role);

        var email = request.Email.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException($"A user with e-mail '{email}' already exists.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            LockoutEnabled = true,
        };

        ThrowIfFailed(await _users.CreateAsync(user, request.Password));
        ThrowIfFailed(await _users.AddToRoleAsync(user, request.Role));

        return await ToDtoAsync(user);
    }

    public async Task<UserDto> ChangeRoleAsync(
        Guid userId,
        string role,
        Guid actingUserId,
        CancellationToken cancellationToken)
    {
        EnsureValidRole(role);
        var user = await FindAsync(userId);
        var currentRoles = await _users.GetRolesAsync(user);

        if (currentRoles.Contains(Roles.Admin) && role != Roles.Admin)
        {
            if (userId == actingUserId)
            {
                throw new ConflictException("You cannot remove your own admin role.");
            }

            await EnsureAnotherActiveAdminExistsAsync(userId, cancellationToken);
        }

        ThrowIfFailed(await _users.RemoveFromRolesAsync(user, currentRoles));
        ThrowIfFailed(await _users.AddToRoleAsync(user, role));

        // Rotating the stamp makes the role change take effect in existing sessions.
        ThrowIfFailed(await _users.UpdateSecurityStampAsync(user));
        return await ToDtoAsync(user);
    }

    public async Task<UserDto> DeactivateAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken)
    {
        if (userId == actingUserId)
        {
            throw new ConflictException("You cannot deactivate your own account.");
        }

        var user = await FindAsync(userId);
        if (await _users.IsInRoleAsync(user, Roles.Admin))
        {
            await EnsureAnotherActiveAdminExistsAsync(userId, cancellationToken);
        }

        user.IsDeactivated = true;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        ThrowIfFailed(await _users.UpdateAsync(user));
        ThrowIfFailed(await _users.UpdateSecurityStampAsync(user));
        return await ToDtoAsync(user);
    }

    public async Task<UserDto> ActivateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindAsync(userId);
        user.IsDeactivated = false;
        user.LockoutEnd = null;
        ThrowIfFailed(await _users.UpdateAsync(user));
        ThrowIfFailed(await _users.ResetAccessFailedCountAsync(user));
        return await ToDtoAsync(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await FindAsync(userId);
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        ThrowIfFailed(await _users.ResetPasswordAsync(user, token, newPassword));
    }

    public async Task ChangeOwnPasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(userId);
        ThrowIfFailed(await _users.ChangePasswordAsync(user, currentPassword, newPassword));
    }

    public async Task<CurrentUserDto> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await FindAsync(userId);
        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = await GetSingleRoleAsync(user),
        };
    }

    private async Task EnsureAnotherActiveAdminExistsAsync(Guid exceptUserId, CancellationToken cancellationToken)
    {
        var admins = await _users.GetUsersInRoleAsync(Roles.Admin);
        var now = _timeProvider.GetUtcNow();
        var othersActive = admins.Any(a =>
            a.Id != exceptUserId && !a.IsDeactivated && (a.LockoutEnd is null || a.LockoutEnd <= now));

        if (!othersActive)
        {
            throw new ConflictException("At least one active admin must remain.");
        }
    }

    private async Task<AppUser> FindAsync(Guid userId)
    {
        return await _users.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User", userId);
    }

    private async Task<string> GetSingleRoleAsync(AppUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return roles.FirstOrDefault() ?? string.Empty;
    }

    private async Task<UserDto> ToDtoAsync(AppUser user)
    {
        var now = _timeProvider.GetUtcNow();
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = await GetSingleRoleAsync(user),
            IsLocked = user.IsDeactivated || (user.LockoutEnd is { } end && end > now),
            CreatedAt = user.CreatedAt,
        };
    }

    private static void EnsureValidRole(string role)
    {
        if (!Roles.IsValid(role))
        {
            throw new ValidationFailedException(
                new[] { $"Role must be one of: {string.Join(", ", Roles.All)}." });
        }
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new ValidationFailedException(result.Errors.Select(e => e.Description).ToList());
        }
    }
}
