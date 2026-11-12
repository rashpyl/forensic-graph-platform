using ForensicGraph.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForensicGraph.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(a => a.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(a => a.UserId).HasColumnName("user_id").HasColumnType("uuid");
        builder.Property(a => a.UserName).HasColumnName("user_name").HasColumnType("text").IsRequired();
        builder.Property(a => a.Action).HasColumnName("action").HasColumnType("text").IsRequired();
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasColumnType("text").IsRequired();
        builder.Property(a => a.EntityId).HasColumnName("entity_id").HasColumnType("text").IsRequired();
        builder.Property(a => a.Changes).HasColumnName("changes").HasColumnType("jsonb").IsRequired();

        builder.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("ix_audit_entity");
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_user");
        builder.HasIndex(a => a.OccurredAt).HasDatabaseName("ix_audit_occurred_at");
    }
}
