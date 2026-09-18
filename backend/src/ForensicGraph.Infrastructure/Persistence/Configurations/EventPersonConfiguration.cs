using ForensicGraph.Domain.CrimeEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForensicGraph.Infrastructure.Persistence.Configurations;

internal sealed class EventPersonConfiguration : IEntityTypeConfiguration<EventPerson>
{
    public void Configure(EntityTypeBuilder<EventPerson> builder)
    {
        builder.ToTable("event_persons");

        builder.HasKey(ep => new { ep.CrimeEventId, ep.PersonId, ep.Role });

        builder.Property(ep => ep.CrimeEventId)
            .HasColumnName("crime_event_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(ep => ep.PersonId)
            .HasColumnName("person_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(ep => ep.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .HasColumnType("int")
            .IsRequired();

        builder.Property(ep => ep.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(ep => ep.PersonId)
            .HasDatabaseName("ix_event_persons_person_id");

        builder.HasIndex(ep => ep.CrimeEventId)
            .HasDatabaseName("ix_event_persons_crime_event_id");
    }
}
