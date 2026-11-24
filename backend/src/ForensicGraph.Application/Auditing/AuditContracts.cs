using ForensicGraph.Application.Common;

namespace ForensicGraph.Application.Auditing;

/// <summary>One recorded change: who did what to which record, and when.</summary>
public sealed record AuditEntryDto
{
    public long Id { get; init; }
    public DateTime OccurredAt { get; init; }
    public Guid? UserId { get; init; }
    public string UserName { get; init; } = string.Empty;

    /// <summary><c>Created</c>, <c>Updated</c> or <c>Deleted</c>.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>For example <c>CrimeEvent</c>, <c>Person</c>, <c>EventPerson</c>, <c>User</c>.</summary>
    public string EntityType { get; init; } = string.Empty;

    /// <summary>Primary key; composite keys are joined with <c>/</c>.</summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>
    /// JSON object mapping each changed property to <c>{ "old": …, "new": … }</c>.
    /// Sensitive values (passport numbers, phones) are stored masked.
    /// </summary>
    public string Changes { get; init; } = "{}";
}

/// <summary>Filters for <c>GET /api/audit</c>. All optional; newest entries first.</summary>
public sealed record AuditQuery
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public Guid? UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;

    public AuditQuery Normalize() => this with
    {
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => PageSize,
        },
    };
}

/// <summary>Read side of the audit log. There is deliberately no write or delete API.</summary>
public interface IAuditLogReader
{
    Task<PagedResult<AuditEntryDto>> ListAsync(AuditQuery query, CancellationToken cancellationToken);
}
