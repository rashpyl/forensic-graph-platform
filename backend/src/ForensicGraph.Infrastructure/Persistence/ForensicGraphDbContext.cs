using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Domain.Persons;
using ForensicGraph.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="DbContext"/> for the Forensic Graph platform.
/// Exposes the investigation aggregates: <see cref="CrimeEvent"/>, <see cref="Person"/>,
/// and the join / link entities <see cref="EventPerson"/> and <see cref="EventLink"/>,
/// plus the ASP.NET Core Identity tables (users, roles, claims) so that accounts
/// live in the same database and transaction scope as the data they change.
/// </summary>
public sealed class ForensicGraphDbContext
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
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

        // Identity defaults to "AspNetUsers" etc.; rename to match the snake_case schema.
        modelBuilder.Entity<AppUser>(b =>
        {
            b.ToTable("users");
            b.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            b.Property(u => u.CreatedAt).HasColumnType("timestamptz");
        });
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForensicGraphDbContext).Assembly);
    }
}
