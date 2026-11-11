using ForensicGraph.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.Persistence;

internal static class SaveChangesExtensions
{
    /// <summary>
    /// Saves changes, turning an EF Core concurrency failure (the row changed
    /// since it was read) into <see cref="ConflictException"/>, which the API maps to 409.
    /// </summary>
    public static async Task<int> SaveChangesWithConflictDetectionAsync(
        this ForensicGraphDbContext db,
        CancellationToken cancellationToken)
    {
        try
        {
            return await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The record was changed by someone else while you were saving. Reload it and apply your changes again.");
        }
    }
}
