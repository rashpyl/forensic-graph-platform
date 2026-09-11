using ForensicGraph.Domain.CrimeEvents;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="DbContext"/> for the Forensic Graph platform.
/// Currently exposes only crime events; further aggregates (Person, Location, Evidence,
/// typed relations) will be added in follow-up changes.
/// </summary>
public sealed class ForensicGraphDbContext : DbContext
{
    public ForensicGraphDbContext(DbContextOptions<ForensicGraphDbContext> options)
        : base(options)
    {
    }

    public DbSet<CrimeEvent> CrimeEvents => Set<CrimeEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForensicGraphDbContext).Assembly);
    }
}
