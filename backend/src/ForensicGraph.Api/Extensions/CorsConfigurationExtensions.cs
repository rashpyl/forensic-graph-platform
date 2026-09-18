namespace ForensicGraph.Api.Extensions;

internal static class CorsConfigurationExtensions
{
    public const string FrontendDevPolicy = "FrontendDev";

    /// <summary>
    /// Registers a CORS policy that allows the Vite dev server
    /// at <c>http://localhost:5173</c> — used only in Development.
    /// Non-Development environments intentionally have no default policy;
    /// production origins must be configured explicitly.
    /// </summary>
    public static IServiceCollection AddForensicGraphCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(FrontendDevPolicy, policy =>
            {
                policy
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        return services;
    }
}
