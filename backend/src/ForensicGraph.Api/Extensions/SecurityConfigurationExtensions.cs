using System.Threading.RateLimiting;
using ForensicGraph.Api.Security;
using ForensicGraph.Application.Security;
using ForensicGraph.Infrastructure.Identity;
using ForensicGraph.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ForensicGraph.Api.Extensions;

/// <summary>
/// Authentication, authorization, CSRF protection and brute-force limits.
///
/// <para><b>Why cookies and not JWT in local storage.</b> The session cookie is
/// <c>HttpOnly</c>, so a script injected into the page (XSS) cannot read it.
/// A token kept in <c>localStorage</c> would be readable by any such script.</para>
///
/// <para><b>CSRF.</b> Because browsers attach cookies automatically, every
/// state-changing request must also carry an antiforgery token in the
/// <c>X-XSRF-TOKEN</c> header. A foreign site cannot read that token, so it
/// cannot forge the request. <c>SameSite=Strict</c> is a second, independent layer.</para>
///
/// <para><b>Secure by default.</b> The fallback policy requires an authenticated
/// user on every endpoint; anonymous access must be opted into explicitly.</para>
/// </summary>
internal static class SecurityConfigurationExtensions
{
    public const string LoginRateLimitPolicy = "login";
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";

    public static IServiceCollection AddForensicGraphSecurity(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services
            .AddIdentityCore<AppUser>(options =>
            {
                // Length over composition rules, following NIST SP 800-63B.
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 4;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ForensicGraphDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        // Re-check the security stamp every minute so that deactivation and role
        // changes reach already signed-in sessions quickly.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "fg.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // An API answers with status codes, not redirects to an HTML login page.
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.CanEdit, p => p.RequireRole(Roles.Admin, Roles.Analyst))
            .AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin));

        services.AddAntiforgery(options =>
        {
            options.HeaderName = AntiforgeryHeaderName;
            options.Cookie.Name = "fg.antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });

        // Validate the antiforgery token on every POST/PUT/PATCH/DELETE handled by a controller.
        services.Configure<MvcOptions>(o => o.Filters.Add<AntiforgeryValidationFilter>());

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Per client IP: at most 5 login attempts per minute. Complements the
            // per-account lockout, which alone would not stop password spraying
            // (one password tried against many accounts).
            options.AddPolicy(LoginRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }

    /// <summary>
    /// Adds defensive HTTP response headers. The API returns JSON only, so the
    /// content security policy forbids everything; Swagger UI (Development only)
    /// is excluded because it needs to run its own scripts.
    /// </summary>
    public static IApplicationBuilder UseForensicGraphSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
                headers["Cache-Control"] = "no-store";
            }

            await next();
        });
    }
}
