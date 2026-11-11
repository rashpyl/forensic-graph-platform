using ForensicGraph.Application.Security;
using ForensicGraph.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForensicGraph.Infrastructure.Seeding;

/// <summary>
/// Creates the three roles and the first administrator.
///
/// <para><b>All environments:</b> roles are ensured on every start. If the
/// <c>users</c> table is empty and <c>Bootstrap:AdminEmail</c> /
/// <c>Bootstrap:AdminPassword</c> are configured (user-secrets or environment
/// variables — never committed), that admin is created. Without them the API
/// still starts, but nobody can sign in until an admin is bootstrapped.</para>
///
/// <para><b>Development only:</b> one demo account per role is created with the
/// password from <c>Seed:DemoPassword</c> so the UI can be tried immediately.</para>
/// </summary>
public static class IdentitySeeder
{
    public static readonly (string Email, string DisplayName, string Role)[] DemoUsers =
    {
        ("admin@forensic.local", "Demo Admin", Roles.Admin),
        ("analyst@forensic.local", "Demo Analyst", Roles.Analyst),
        ("viewer@forensic.local", "Demo Viewer", Roles.Viewer),
    };

    public static async Task SeedAsync(
        IServiceProvider services,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ForensicGraph.IdentitySeeder");

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
            }
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (isDevelopment)
        {
            var demoPassword = configuration["Seed:DemoPassword"];
            if (string.IsNullOrWhiteSpace(demoPassword))
            {
                logger.LogWarning("Seed:DemoPassword is not set; demo accounts were not created.");
            }
            else
            {
                foreach (var (email, displayName, role) in DemoUsers)
                {
                    await EnsureUserAsync(userManager, email, displayName, role, demoPassword, now, logger);
                }
            }
        }

        if (userManager.Users.Any())
        {
            return;
        }

        var adminEmail = configuration["Bootstrap:AdminEmail"];
        var adminPassword = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "No user accounts exist. Set Bootstrap:AdminEmail and Bootstrap:AdminPassword " +
                "to create the first administrator.");
            return;
        }

        await EnsureUserAsync(userManager, adminEmail, "Administrator", Roles.Admin, adminPassword, now, logger);
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string displayName,
        string role,
        string password,
        DateTime nowUtc,
        ILogger logger)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            CreatedAt = nowUtc,
            LockoutEnabled = true,
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            logger.LogError(
                "Could not create user {Email}: {Errors}",
                email,
                string.Join(" ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Created {Role} account {Email}.", role, email);
    }
}
