using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForensicGraph.Application.Security;
using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Domain.Persons;
using ForensicGraph.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ForensicGraph.Infrastructure.Auditing;

/// <summary>
/// Adds an <c>audit_entries</c> row for every create, update and delete of investigation
/// data and user accounts. The row is saved in the same <c>SaveChanges</c> call, so the
/// change and its audit entry are stored together or not at all.
/// <list type="bullet">
///   <item>Updates record only the properties that changed, as <c>{ old, new }</c>.</item>
///   <item>Deletes record the full last state, so the record can be reconstructed.</item>
///   <item>Passport numbers and phones are stored masked, so the log is not a second,
///         less protected copy of them.</item>
///   <item>For users only email, display name and deactivation are recorded; password
///         hashes, security stamps and lockout counters never reach the log.</item>
/// </list>
/// Rows removed by a database-level cascade are logged only if they were loaded. The
/// repositories load and remove dependent rows explicitly on delete, so the cascade is
/// only a safety net for deletes made outside the application.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.Ordinal)
    {
        nameof(Person.PassportNumbers),
        nameof(Person.Phone),
    };

    private static readonly HashSet<string> AuditedUserProperties = new(StringComparer.Ordinal)
    {
        nameof(AppUser.Email),
        nameof(AppUser.DisplayName),
        nameof(AppUser.IsDeactivated),
    };

    /// <summary>Technical columns that change on every write and carry no information.</summary>
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.Ordinal)
    {
        nameof(CrimeEvent.Version),
        nameof(CrimeEvent.UpdatedAt),
    };

    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            AddAuditEntries(context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            AddAuditEntries(context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext context)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var userName = _currentUser.UserName ?? "system";

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => BuildEntry(context, e, now, userName))
            .OfType<AuditEntry>()
            .ToList();

        if (entries.Count > 0)
        {
            context.Set<AuditEntry>().AddRange(entries);
        }
    }

    private AuditEntry? BuildEntry(DbContext context, EntityEntry entry, DateTime now, string userName)
    {
        var entityType = AuditedTypeName(entry.Entity);
        if (entityType is null)
        {
            return null;
        }

        var changes = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (!ShouldRecord(entry.Entity, name))
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    changes[name] = new { old = (object?)null, @new = Protect(name, property.CurrentValue) };
                    break;
                case EntityState.Deleted:
                    changes[name] = new { old = Protect(name, property.OriginalValue), @new = (object?)null };
                    break;
                case EntityState.Modified when property.IsModified && !ValuesEqual(property.OriginalValue, property.CurrentValue):
                    changes[name] = new
                    {
                        old = Protect(name, property.OriginalValue),
                        @new = Protect(name, property.CurrentValue),
                    };
                    break;
            }
        }

        if (entry.Entity is IdentityUserRole<Guid> userRole)
        {
            changes["Role"] = new { value = ResolveRoleName(context, userRole.RoleId) };
        }

        // An update that touched only ignored fields (e.g. a login updating the
        // user's lockout counters) is not worth an audit row.
        if (entry.State == EntityState.Modified && changes.Count == 0)
        {
            return null;
        }

        return new AuditEntry
        {
            OccurredAt = now,
            UserId = _currentUser.UserId,
            UserName = userName,
            Action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Deleted => "Deleted",
                _ => "Updated",
            },
            EntityType = entityType,
            EntityId = string.Join("/", entry.Metadata.FindPrimaryKey()!.Properties
                .Select(p => Convert.ToString(entry.Property(p.Name).CurrentValue, System.Globalization.CultureInfo.InvariantCulture))),
            Changes = JsonSerializer.Serialize(changes, JsonOptions),
        };
    }

    private static string? AuditedTypeName(object entity) => entity switch
    {
        CrimeEvent => nameof(CrimeEvent),
        Person => nameof(Person),
        EventPerson => nameof(EventPerson),
        EventLink => nameof(EventLink),
        AppUser => "User",
        IdentityUserRole<Guid> => "UserRole",
        _ => null,
    };

    private static bool ShouldRecord(object entity, string propertyName)
    {
        if (IgnoredProperties.Contains(propertyName))
        {
            return false;
        }

        return entity switch
        {
            AppUser => AuditedUserProperties.Contains(propertyName),
            _ => true,
        };
    }

    private static object? Protect(string propertyName, object? value)
    {
        if (!SensitiveProperties.Contains(propertyName) || value is null)
        {
            return value;
        }

        return value switch
        {
            string s => SensitiveDataMasker.MaskEach(s),
            IEnumerable items => items.Cast<object?>()
                .Select(i => i is null ? null : SensitiveDataMasker.Mask(i.ToString() ?? string.Empty))
                .ToList(),
            _ => "•••",
        };
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a is IEnumerable ea && b is IEnumerable eb && a is not string && b is not string)
        {
            return ea.Cast<object?>().SequenceEqual(eb.Cast<object?>());
        }

        return Equals(a, b);
    }

    private static string ResolveRoleName(DbContext context, Guid roleId)
    {
        var role = context.Set<IdentityRole<Guid>>().Local.FirstOrDefault(r => r.Id == roleId)
            ?? context.Set<IdentityRole<Guid>>().AsNoTracking().FirstOrDefault(r => r.Id == roleId);
        return role?.Name ?? roleId.ToString();
    }
}
