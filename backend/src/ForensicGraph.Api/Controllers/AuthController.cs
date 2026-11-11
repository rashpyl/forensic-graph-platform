using ForensicGraph.Api.Extensions;
using ForensicGraph.Application.Security;
using ForensicGraph.Application.Users;
using ForensicGraph.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ForensicGraph.Api.Controllers;

/// <summary>
/// Sign-in, sign-out, the current user's profile and the antiforgery token.
///
/// Typical client flow: <c>GET /api/auth/csrf</c> → <c>POST /api/auth/login</c>
/// with the token in <c>X-XSRF-TOKEN</c> → <c>GET /api/auth/csrf</c> again,
/// because the token is bound to the signed-in identity.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private const string InvalidCredentials = "Invalid e-mail or password.";

    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;
    private readonly IUserAdministrationService _userAdmin;
    private readonly ICurrentUser _currentUser;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<AppUser> signIn,
        UserManager<AppUser> users,
        IUserAdministrationService userAdmin,
        ICurrentUser currentUser,
        IAntiforgery antiforgery,
        ILogger<AuthController> logger)
    {
        _signIn = signIn;
        _users = users;
        _userAdmin = userAdmin;
        _currentUser = currentUser;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    /// <summary>
    /// Issues an antiforgery token. The client sends it back in the
    /// <c>X-XSRF-TOKEN</c> header on every state-changing request.
    /// </summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CsrfTokenResponse), StatusCodes.Status200OK)]
    public ActionResult<CsrfTokenResponse> Csrf()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return new CsrfTokenResponse(tokens.RequestToken ?? string.Empty);
    }

    /// <summary>
    /// Signs in with e-mail and password and sets the session cookie.
    /// The error message is the same for an unknown e-mail, a wrong password and
    /// a locked account, so the endpoint cannot be used to discover accounts.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityConfigurationExtensions.LoginRateLimitPolicy)]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CurrentUserDto>> Login(
        [FromBody] LoginRequest body,
        CancellationToken cancellationToken)
    {
        var user = await _users.FindByEmailAsync(body.Email.Trim());
        if (user is null)
        {
            _logger.LogWarning("Failed login for unknown account.");
            return Unauthorized(Problem401());
        }

        var result = await _signIn.PasswordSignInAsync(
            user, body.Password, isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Failed login for user {UserId} (locked out: {LockedOut}).", user.Id, result.IsLockedOut);
            return Unauthorized(Problem401());
        }

        _logger.LogInformation("User {UserId} signed in.", user.Id);
        return await _userAdmin.GetCurrentAsync(user.Id, cancellationToken);
    }

    /// <summary>Signs out and clears the session cookie.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return NoContent();
    }

    /// <summary>Returns the signed-in user, or 401 when there is no valid session.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<CurrentUserDto> Me(CancellationToken cancellationToken)
    {
        return _userAdmin.GetCurrentAsync(RequireUserId(), cancellationToken);
    }

    /// <summary>Changes the signed-in user's own password.</summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangeOwnPasswordRequest body,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        await _userAdmin.ChangeOwnPasswordAsync(userId, body.CurrentPassword, body.NewPassword, cancellationToken);

        // Refresh the cookie: changing the password rotates the security stamp.
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is not null)
        {
            await _signIn.RefreshSignInAsync(user);
        }

        return NoContent();
    }

    private Guid RequireUserId() =>
        _currentUser.UserId ?? throw new UnauthorizedAccessException("No authenticated user.");

    private static ProblemDetails Problem401() => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = InvalidCredentials,
    };
}

public sealed record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record CsrfTokenResponse(string Token);
