using ForensicGraph.Application.Auditing;
using ForensicGraph.Application.Common;
using ForensicGraph.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForensicGraph.Api.Controllers;

/// <summary>Read-only access to the audit log. Admin only.</summary>
[ApiController]
[Route("api/audit")]
[Produces("application/json")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditLogReader _reader;

    public AuditController(IAuditLogReader reader)
    {
        _reader = reader;
    }

    /// <summary>
    /// Lists audit entries, newest first. Filter by <c>entityType</c>, <c>entityId</c>
    /// (also matches composite keys containing it) or <c>userId</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditEntryDto>), StatusCodes.Status200OK)]
    public Task<PagedResult<AuditEntryDto>> List([FromQuery] AuditQuery query, CancellationToken cancellationToken)
    {
        return _reader.ListAsync(query, cancellationToken);
    }
}
