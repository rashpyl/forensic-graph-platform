using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Domain.Persons;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="DbContext"/> for the Forensic Graph platform.
/// Exposes the investigation aggregates: <see cref="CrimeEvent"/>, <see cref="Person"/>,
/// and the join / link entities <see cref="EventPerson"/> and <see cref="EventLink"/>.
/// </summary>
public sealed class ForensicGraphDbContext : DbContext
{
    public ForensicGraphDbContext(DbContextOptions<ForensicGraphDbContext> options)
        : base(options)
    {
    }

    public DbSet<CrimeEvent> CrimeEvents => Set<CrimeEvent>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<EventPerson> EventPersons => Set<EventPerson>();
    public DbSet<EventLink> EventLinks => Set<EventLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForensicGraphDbContext).Assembly);
    }
}
