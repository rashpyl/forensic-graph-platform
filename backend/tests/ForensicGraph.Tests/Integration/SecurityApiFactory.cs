using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace ForensicGraph.Tests.Integration;

/// <summary>
/// Boots the real API (Development environment, so migrations run and the demo
/// accounts are seeded) against a throw-away PostgreSQL database that is
/// created for the test run and dropped afterwards.
///
/// Integration tests run only when <c>FG_TEST_POSTGRES</c> holds a connection
/// string to a server where the user may create databases, for example
/// <c>Host=localhost;Port=5433;Username=forensic;Password=forensic</c>
/// (the docker-compose container). Otherwise they are skipped, so
/// <c>dotnet test</c> keeps working without a database.
/// </summary>
public sealed class SecurityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoPassword = "integration-demo-password";
    private const string EnvVar = "FG_TEST_POSTGRES";

    private readonly string _databaseName = $"fg_test_{Guid.NewGuid():N}";

    public static string? ServerConnectionString => Environment.GetEnvironmentVariable(EnvVar);

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ServerConnectionString);

    public string DatabaseConnectionString =>
        new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = _databaseName }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgres", DatabaseConnectionString);
        builder.UseSetting("Seed:DemoPassword", DemoPassword);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        if (!IsConfigured)
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        // Connect to the maintenance database to drop the test database.
        var maintenance = new NpgsqlConnectionStringBuilder(ServerConnectionString) { Database = "postgres" };
        await using var conn = new NpgsqlConnection(maintenance.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>A client with its own cookie jar, already signed in as <paramref name="email"/>.</summary>
    public async Task<SessionClient> SignInAsync(string email, string password = DemoPassword)
    {
        var session = new SessionClient(CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }));
        var response = await session.PostAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        session.ResetCsrf();
        return session;
    }

    public SessionClient Anonymous() =>
        new(CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }));
}

/// <summary>
/// Wraps an <see cref="HttpClient"/> the way the frontend behaves: it fetches an
/// antiforgery token and sends it on every state-changing request.
/// </summary>
public sealed class SessionClient
{
    private string? _csrf;

    public SessionClient(HttpClient http)
    {
        Http = http;
    }

    public HttpClient Http { get; }

    public void ResetCsrf() => _csrf = null;

    public Task<HttpResponseMessage> GetAsync(string url) => Http.GetAsync(url);

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null) =>
        SendAsync(HttpMethod.Post, url, body);

    public Task<HttpResponseMessage> PutAsync(string url, object body) =>
        SendAsync(HttpMethod.Put, url, body);

    public Task<HttpResponseMessage> DeleteAsync(string url) =>
        SendAsync(HttpMethod.Delete, url, null);

    /// <summary>Sends without the antiforgery header, as a forged cross-site request would.</summary>
    public Task<HttpResponseMessage> PostWithoutCsrfAsync(string url, object body) =>
        Http.PostAsJsonAsync(url, body);

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body)
    {
        _csrf ??= (await Http.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf"))!.Token;
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-XSRF-TOKEN", _csrf);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await Http.SendAsync(request);
    }

    private sealed record CsrfResponse(string Token);
}
