using ForensicGraph.Application.Security;
using ForensicGraph.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForensicGraph.Api.Controllers;

/// <summary>
/// User account administration. Admin only. Accounts are deactivated, never
/// deleted, so audit entries always refer to an existing user.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserAdministrationService _service;
    private readonly ICurrentUser _currentUser;

    public UsersController(IUserAdministrationService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UserDto>> List(CancellationToken cancellationToken)
    {
        return _service.ListAsync(cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest body,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(body, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<UserDto> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequest body,
        CancellationToken cancellationToken)
    {
        return _service.ChangeRoleAsync(id, body.Role, ActingUserId, cancellationToken);
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<UserDto> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        return _service.DeactivateAsync(id, ActingUserId, cancellationToken);
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public Task<UserDto> Activate(Guid id, CancellationToken cancellationToken)
    {
        return _service.ActivateAsync(id, cancellationToken);
    }

    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest body,
        CancellationToken cancellationToken)
    {
        await _service.ResetPasswordAsync(id, body.NewPassword, cancellationToken);
        return NoContent();
    }

    private Guid ActingUserId =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("No authenticated user.");
}
