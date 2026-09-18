using ForensicGraph.Domain.CrimeEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForensicGraph.Infrastructure.Persistence.Configurations;

internal sealed class CrimeEventConfiguration : IEntityTypeConfiguration<CrimeEvent>
{
    public void Configure(EntityTypeBuilder<CrimeEvent> builder)
    {
        builder.ToTable("crime_events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(e => e.Address)
            .HasColumnName("address")
            .HasColumnType("text");

        builder.Property(e => e.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasColumnType("int")
            .IsRequired();

        builder.Property(e => e.Latitude)
            .HasColumnName("latitude")
            .HasColumnType("double precision");

        builder.Property(e => e.Longitude)
            .HasColumnName("longitude")
            .HasColumnType("double precision");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(e => e.OccurredAt)
            .HasDatabaseName("ix_crime_events_occurred_at");

        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("ix_crime_events_severity");

        builder.HasMany(e => e.Persons)
            .WithOne()
            .HasForeignKey(ep => ep.CrimeEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.OutgoingLinks)
            .WithOne()
            .HasForeignKey(l => l.FromEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
