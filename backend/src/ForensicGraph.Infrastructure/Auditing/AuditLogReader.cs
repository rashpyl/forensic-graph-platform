using ForensicGraph.Application.Auditing;
using ForensicGraph.Application.Common;
using ForensicGraph.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.Auditing;

/// <summary>EF Core-backed <see cref="IAuditLogReader"/>. Read-only, newest entries first.</summary>
public sealed class AuditLogReader : IAuditLogReader
{
    private readonly ForensicGraphDbContext _db;

    public AuditLogReader(ForensicGraphDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditEntryDto>> ListAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        var normalized = query.Normalize();
        IQueryable<AuditEntry> q = _db.Set<AuditEntry>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalized.EntityType))
        {
            q = q.Where(a => a.EntityType == normalized.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(normalized.EntityId))
        {
            // Matches the record itself and composite keys that contain it,
            // e.g. an event id also finds the EventPerson rows of that event.
            var id = normalized.EntityId;
            q = q.Where(a => a.EntityId == id || a.EntityId.StartsWith(id + "/") || a.EntityId.Contains("/" + id));
        }

        if (normalized.UserId is { } userId)
        {
            q = q.Where(a => a.UserId == userId);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.Id)
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(a => new AuditEntryDto
            {
                Id = a.Id,
                OccurredAt = a.OccurredAt,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Changes = a.Changes,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntryDto>(items, total, normalized.Page, normalized.PageSize);
    }
}
