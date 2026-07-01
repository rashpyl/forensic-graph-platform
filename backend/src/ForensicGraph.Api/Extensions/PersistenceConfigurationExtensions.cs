using ForensicGraph.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Api.Extensions;

internal static class PersistenceConfigurationExtensions
{
    /// <summary>
    /// Registers <see cref="ForensicGraphDbContext"/> with the Npgsql provider.
    /// The connection string is read from <c>ConnectionStrings:Postgres</c>; startup
    /// fails fast when it is missing so the app never boots against an unconfigured database.
    /// </summary>
    public static IServiceCollection AddForensicGraphPersistence(
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

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations in Development.
    /// In non-Development environments the app fails fast if any migration is pending,
    /// so operators are forced to run <c>dotnet ef database update</c> explicitly.
    /// </summary>
    public static async Task ApplyForensicGraphMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForensicGraphDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ForensicGraph.Migrations");

        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Applying EF Core migrations (Development).");
            await db.Database.MigrateAsync();
            return;
        }

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
        if (pending.Length == 0)
        {
            return;
        }

        logger.LogCritical(
            "Refusing to start: {Count} pending EF Core migration(s): {Migrations}. " +
            "Run 'dotnet ef database update' before deploying.",
            pending.Length,
            string.Join(", ", pending));

        throw new InvalidOperationException(
            $"Pending EF Core migrations detected: {string.Join(", ", pending)}");
    }
}
