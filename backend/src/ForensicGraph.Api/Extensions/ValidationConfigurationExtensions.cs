using FluentValidation;
using FluentValidation.AspNetCore;

namespace ForensicGraph.Api.Extensions;

internal static class ValidationConfigurationExtensions
{
    /// <summary>
    /// Registers FluentValidation with assembly scanning of ForensicGraph.Application
    /// and enables automatic MVC model validation. Validation failures surface as
    /// RFC 7807 ProblemDetails 400 responses via the framework's built-in behavior.
    /// </summary>
    public static IServiceCollection AddForensicGraphValidation(this IServiceCollection services)
    {
        var applicationAssembly = typeof(Application.AssemblyMarker).Assembly;
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddFluentValidationAutoValidation();
        return services;
    }
}
