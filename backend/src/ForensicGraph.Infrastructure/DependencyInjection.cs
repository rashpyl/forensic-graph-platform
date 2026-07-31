using ForensicGraph.Application.CrimeEvents;
using ForensicGraph.Application.Persons;
using ForensicGraph.Infrastructure.CrimeEvents;
using ForensicGraph.Infrastructure.Persistence;
using ForensicGraph.Infrastructure.Persons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForensicGraph.Infrastructure;

/// <summary>
/// Composition-root extension that wires the persistence stack (DbContext,
/// repositories) and the application-layer services in a single call.
/// The API project should call <c>services.AddForensicGraphInfrastructure(configuration)</c>
/// in <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddForensicGraphInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured. " +
                "Set it via user-secrets, environment variables, or appsettings.");
        }

        services.AddDbContext<ForensicGraphDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(
                typeof(ForensicGraphDbContext).Assembly.GetName().Name)));

        services.AddScoped<ICrimeEventRepository, CrimeEventRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();

        services.AddScoped<CrimeEventService>();
        services.AddScoped<PersonService>();

        return services;
    }
}
