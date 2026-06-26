using ForensicGraph.Api.Extensions;
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

builder.Services.AddForensicGraphValidation();
builder.Services.AddForensicGraphSwagger();
builder.Services.AddForensicGraphCors();

var app = builder.Build();

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
