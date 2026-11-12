namespace ForensicGraph.Infrastructure.Auditing;

/// <summary>
/// Row of the append-only <c>audit_entries</c> table. Written only by
/// <see cref="AuditSaveChangesInterceptor"/>; a database trigger rejects any
/// UPDATE or DELETE, so not even the application can rewrite history.
/// </summary>
public sealed class AuditEntry
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Changes { get; set; } = "{}";
}
