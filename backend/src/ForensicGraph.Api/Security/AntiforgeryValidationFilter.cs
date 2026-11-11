using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ForensicGraph.Api.Security;

/// <summary>
/// Global MVC filter that rejects state-changing requests (POST, PUT, PATCH, DELETE)
/// whose <c>X-XSRF-TOKEN</c> header does not match the antiforgery cookie.
/// Safe methods (GET, HEAD, OPTIONS) are not checked because they must not change state.
///
/// A dedicated filter is used instead of MVC's <c>AutoValidateAntiforgeryToken</c>
/// because that one depends on the view (Razor) stack, which this JSON API does not load.
/// </summary>
internal sealed class AntiforgeryValidationFilter : IAsyncAuthorizationFilter
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<AntiforgeryValidationFilter> _logger;

    public AntiforgeryValidationFilter(IAntiforgery antiforgery, ILogger<AntiforgeryValidationFilter> logger)
    {
        _antiforgery = antiforgery;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (SafeMethods.Contains(context.HttpContext.Request.Method))
        {
            return;
        }

        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException ex)
        {
            _logger.LogWarning("Rejected {Method} {Path}: {Reason}",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path, ex.Message);

            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing or invalid antiforgery token.",
                Detail = "Request a token from GET /api/auth/csrf and send it in the X-XSRF-TOKEN header.",
            })
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }
    }
}
