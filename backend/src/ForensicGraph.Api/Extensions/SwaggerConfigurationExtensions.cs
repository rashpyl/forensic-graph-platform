using Microsoft.OpenApi.Models;

namespace ForensicGraph.Api.Extensions;

internal static class SwaggerConfigurationExtensions
{
    public static IServiceCollection AddForensicGraphSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Forensic Graph API",
                Version = "v1",
                Description = "REST API for the Forensic Graph Platform.",
            });
        });
        return services;
    }

    public static IApplicationBuilder UseForensicGraphSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Forensic Graph API v1");
            options.RoutePrefix = "swagger";
        });
        return app;
    }
}
