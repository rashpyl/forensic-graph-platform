using System.Security.Claims;
using ForensicGraph.Application.Security;

namespace ForensicGraph.Api.Security;

/// <summary>
/// <see cref="ICurrentUser"/> backed by the authentication cookie of the
/// current HTTP request. Outside a request (startup seeding, background work)
/// every member reports "no user".
/// </summary>
internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal?.Identity?.IsAuthenticated == true
        ? Principal.Identity.Name
        : null;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
