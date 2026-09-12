using ForensicGraph.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace ForensicGraph.Api.Extensions;

/// <summary>
/// Global exception handler that maps unhandled exceptions to RFC 7807
/// ProblemDetails responses. Domain-signalled errors map to intent-preserving
/// HTTP status codes:
/// <list type="bullet">
///   <item><see cref="NotFoundException"/> → 404</item>
///   <item><see cref="ConflictException"/> → 409</item>
///   <item>anything else → 500</item>
/// </list>
/// Stack traces are only emitted for 500s in Development.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = Map(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation(
                "Handled {Exception} on {Method} {Path} → {Status}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                status);
        }

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Instance = httpContext.Request.Path,
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            if (environment.IsDevelopment())
            {
                problem.Detail = exception.ToString();
            }
        }
        else
        {
            problem.Detail = exception.Message;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static (int Status, string Title, string Type) Map(Exception exception) => exception switch
    {
        NotFoundException => (
            StatusCodes.Status404NotFound,
            "Resource not found.",
            "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"),
        ConflictException => (
            StatusCodes.Status409Conflict,
            "Request conflicts with the current state.",
            "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8"),
        _ => (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"),
    };
}
