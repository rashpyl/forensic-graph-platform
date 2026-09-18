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

var app = builder.Build();

await app.ApplyForensicGraphMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    await DatabaseSeeder.SeedAsync(app.Services);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseCors(CorsConfigurationExtensions.FrontendDevPolicy);
    app.UseForensicGraphSwagger();
}

app.MapControllers();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }))
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
