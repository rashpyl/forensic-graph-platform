using ForensicGraph.Domain.Persons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ForensicGraph.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("persons");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.Phone)
            .HasColumnName("phone")
            .HasColumnType("text");

        builder.Property(p => p.PhysicalDescription)
            .HasColumnName("physical_description")
            .HasColumnType("text");

        builder.Property(p => p.Citizenships)
            .HasColumnName("citizenships")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(p => p.PassportNumbers)
            .HasColumnName("passport_numbers")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(p => new { p.LastName, p.FirstName })
            .HasDatabaseName("ix_persons_last_first_name");
    }
}
