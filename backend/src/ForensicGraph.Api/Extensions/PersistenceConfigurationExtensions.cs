using ForensicGraph.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Api.Extensions;

internal static class PersistenceConfigurationExtensions
{
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
