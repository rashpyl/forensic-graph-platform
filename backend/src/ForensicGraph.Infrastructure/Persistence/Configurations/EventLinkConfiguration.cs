using ForensicGraph.Domain.CrimeEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForensicGraph.Infrastructure.Persistence.Configurations;

internal sealed class EventLinkConfiguration : IEntityTypeConfiguration<EventLink>
{
    public void Configure(EntityTypeBuilder<EventLink> builder)
    {
        builder.ToTable("event_links");

        builder.HasKey(l => new { l.FromEventId, l.ToEventId });

        builder.Property(l => l.FromEventId)
            .HasColumnName("from_event_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(l => l.ToEventId)
            .HasColumnName("to_event_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(l => l.Note)
            .HasColumnName("note")
            .HasColumnType("text");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(l => l.FromEventId)
            .HasDatabaseName("ix_event_links_from_event_id");

        builder.HasIndex(l => l.ToEventId)
            .HasDatabaseName("ix_event_links_to_event_id");
    }
}
