using System.Text.Json.Serialization;
using ForensicGraph.Api.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ForensicGraph.Api.Extensions;

internal static class JsonConfigurationExtensions
{
    /// <summary>
    /// Configures JSON serialization for MVC:
    /// camelCase property names, ISO-8601 UTC datetimes ending in 'Z',
    /// enums as strings, and a stable ProblemDetails shape.
    /// Guids are already emitted lowercase by <c>System.Text.Json</c>.
    /// </summary>
    public static IMvcBuilder AddForensicGraphJson(this IMvcBuilder builder)
    {
        return builder.AddJsonOptions(options =>
        {
            var opts = options.JsonSerializerOptions;
            opts.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opts.PropertyNameCaseInsensitive = true;
            opts.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            opts.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
            opts.Converters.Add(new Iso8601UtcDateTimeConverter());
        });
    }
}
