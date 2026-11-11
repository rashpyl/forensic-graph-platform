using ForensicGraph.Api.Extensions;
using ForensicGraph.Infrastructure;
using ForensicGraph.Infrastructure.Seeding;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

builder.Services
    .AddControllers()
    .AddForensicGraphJson();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddForensicGraphValidation();
builder.Services.AddForensicGraphSwagger();
builder.Services.AddForensicGraphCors();
builder.Services.AddForensicGraphInfrastructure(builder.Configuration);
builder.Services.AddForensicGraphSecurity(builder.Environment);

var app = builder.Build();

await app.ApplyForensicGraphMigrationsAsync();
await IdentitySeeder.SeedAsync(app.Services, app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    await DatabaseSeeder.SeedAsync(app.Services);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseForensicGraphSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseCors(CorsConfigurationExtensions.FrontendDevPolicy);
    app.UseForensicGraphSwagger();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
   .AllowAnonymous()
   .WithName("HealthCheck")
   .WithTags("System");

try
{
    Log.Information("Starting Forensic Graph API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Forensic Graph API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
