using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace ForensicGraph.Tests.Integration;

/// <summary>
/// End-to-end checks of the security requirements against the real HTTP
/// pipeline and a real PostgreSQL database: authentication, role-based
/// authorization, CSRF protection, sensitive-field masking, optimistic
/// concurrency and the append-only audit log.
///
/// Sign-ins happen once per role in the shared fixture because the login
/// endpoint is rate-limited per client address.
/// </summary>
public sealed class SecurityIntegrationTests : IClassFixture<SecurityIntegrationTests.Sessions>
{
    private readonly Sessions _s;

    public SecurityIntegrationTests(Sessions sessions)
    {
        _s = sessions;
    }

    [SkippableFact]
    public async Task Anonymous_request_is_rejected()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");

        var response = await _s.Factory.Anonymous().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task Health_check_is_public()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");

        var response = await _s.Factory.Anonymous().GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task Wrong_password_and_unknown_user_get_identical_answers()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        var anon = _s.Factory.Anonymous();

        var wrongPassword = await anon.PostAsync("/api/auth/login", new { email = "viewer@forensic.local", password = "not-the-password" });
        var unknownUser = await anon.PostAsync("/api/auth/login", new { email = "nobody@forensic.local", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(await TitleOf(wrongPassword), await TitleOf(unknownUser));
    }

    [SkippableFact]
    public async Task Viewer_can_read_but_not_write()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");

        var read = await _s.Viewer.GetAsync("/api/events");
        var write = await _s.Viewer.PostAsync("/api/persons", new { firstName = "X", lastName = "Y" });

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [SkippableFact]
    public async Task Only_admin_can_manage_users_and_read_audit()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");

        Assert.Equal(HttpStatusCode.Forbidden, (await _s.Analyst.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _s.Analyst.GetAsync("/api/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _s.Admin.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _s.Admin.GetAsync("/api/audit")).StatusCode);
    }

    [SkippableFact]
    public async Task Write_without_antiforgery_token_is_rejected()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");

        var response = await _s.Analyst.PostWithoutCsrfAsync("/api/persons", new { firstName = "Forged", lastName = "Request" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task Viewer_gets_masked_passport_analyst_gets_full_value()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        var created = await CreatePersonAsync("AB1234512", "+420111222333");

        var asViewer = await _s.Viewer.Http.GetFromJsonAsync<JsonElement>($"/api/persons/{created.Id}");
        var asAnalyst = await _s.Analyst.Http.GetFromJsonAsync<JsonElement>($"/api/persons/{created.Id}");

        Assert.Equal("AB•••••12", asViewer.GetProperty("passportNumbers")[0].GetString());
        Assert.DoesNotContain("1112223", asViewer.GetProperty("phone").GetString());
        Assert.Equal("AB1234512", asAnalyst.GetProperty("passportNumbers")[0].GetString());
    }

    [SkippableFact]
    public async Task Stale_update_is_rejected_and_does_not_overwrite()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        var person = await CreatePersonAsync("CD0000001", null);

        var first = await _s.Analyst.PutAsync($"/api/persons/{person.Id}", PersonBody("First edit", person.Version));
        var second = await _s.Admin.PutAsync($"/api/persons/{person.Id}", PersonBody("Second edit", person.Version));
        var current = await _s.Analyst.Http.GetFromJsonAsync<JsonElement>($"/api/persons/{person.Id}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("First edit", current.GetProperty("firstName").GetString());
    }

    [SkippableFact]
    public async Task Change_is_audited_with_user_and_masked_values()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        var person = await CreatePersonAsync("EF7654321", null);

        var page = await _s.Admin.Http.GetFromJsonAsync<JsonElement>($"/api/audit?entityId={person.Id}");
        var entry = page.GetProperty("items").EnumerateArray().Single();
        var changes = entry.GetProperty("changes").GetString()!;

        Assert.Equal("analyst@forensic.local", entry.GetProperty("userName").GetString());
        Assert.Equal("Created", entry.GetProperty("action").GetString());
        Assert.Contains("EF•••••21", changes);
        Assert.DoesNotContain("EF7654321", changes);
    }

    [SkippableFact]
    public async Task Audit_log_cannot_be_modified_even_directly_in_the_database()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        await CreatePersonAsync(null, null);

        await using var conn = new NpgsqlConnection(_s.Factory.DatabaseConnectionString);
        await conn.OpenAsync();

        await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var cmd = new NpgsqlCommand("DELETE FROM audit_entries", conn);
            await cmd.ExecuteNonQueryAsync();
        });
        await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var cmd = new NpgsqlCommand("UPDATE audit_entries SET user_name = 'someone else'", conn);
            await cmd.ExecuteNonQueryAsync();
        });
    }

    [SkippableFact]
    public async Task Deleting_person_audits_each_removed_assignment()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        var person = await CreatePersonAsync(null, null);
        var first = await CreateEventAsync();
        var second = await CreateEventAsync();
        await AssignAsync(first, person.Id, "Suspect");
        await AssignAsync(second, person.Id, "Witness");

        var delete = await _s.Analyst.DeleteAsync($"/api/persons/{person.Id}");
        var deleted = await DeletedEntriesAsync(person.Id);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(new[] { "EventPerson", "EventPerson", "Person" }, deleted.OrderBy(t => t).ToArray());
    }

    [SkippableFact]
    public async Task Deleting_event_audits_assignments_and_both_link_directions()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        var person = await CreatePersonAsync(null, null);
        var target = await CreateEventAsync();
        var outgoingTarget = await CreateEventAsync();
        var incomingSource = await CreateEventAsync();
        await AssignAsync(target, person.Id, "Victim");
        await LinkAsync(target, outgoingTarget);
        await LinkAsync(incomingSource, target);

        var delete = await _s.Analyst.DeleteAsync($"/api/events/{target}");
        var deleted = await DeletedEntriesAsync(target);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(
            new[] { "CrimeEvent", "EventLink", "EventLink", "EventPerson" },
            deleted.OrderBy(t => t).ToArray());
    }

    [SkippableFact]
    public async Task Admin_cannot_lock_themselves_out()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        var me = await _s.Admin.Http.GetFromJsonAsync<JsonElement>("/api/auth/me");
        var myId = me.GetProperty("id").GetString();

        var deactivate = await _s.Admin.PostAsync($"/api/users/{myId}/deactivate");
        var demote = await _s.Admin.PutAsync($"/api/users/{myId}/role", new { role = "Viewer" });

        Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
    }

    private async Task<(Guid Id, uint Version)> CreatePersonAsync(string? passport, string? phone)
    {
        var response = await _s.Analyst.PostAsync("/api/persons", new
        {
            firstName = "Test",
            lastName = "Person",
            citizenships = new[] { "CZ" },
            passportNumbers = passport is null ? Array.Empty<string>() : new[] { passport },
            phone,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("id").GetGuid(), body.GetProperty("version").GetUInt32());
    }

    private async Task<Guid> CreateEventAsync()
    {
        var response = await _s.Analyst.PostAsync("/api/events", new
        {
            title = "Audit test event",
            occurredAt = DateTime.UtcNow.AddDays(-1),
            severity = 2,
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task AssignAsync(Guid eventId, Guid personId, string role) =>
        (await _s.Analyst.PostAsync($"/api/events/{eventId}/persons", new { personId, role }))
            .EnsureSuccessStatusCode();

    private async Task LinkAsync(Guid fromEventId, Guid toEventId) =>
        (await _s.Analyst.PostAsync($"/api/events/{fromEventId}/links", new { toEventId }))
            .EnsureSuccessStatusCode();

    private async Task<List<string>> DeletedEntriesAsync(Guid entityId)
    {
        var page = await _s.Admin.Http.GetFromJsonAsync<JsonElement>($"/api/audit?entityId={entityId}&pageSize=200");
        return page.GetProperty("items").EnumerateArray()
            .Where(e => e.GetProperty("action").GetString() == "Deleted")
            .Select(e => e.GetProperty("entityType").GetString()!)
            .ToList();
    }

    private static object PersonBody(string firstName, uint version) => new
    {
        firstName,
        lastName = "Person",
        citizenships = new[] { "CZ" },
        passportNumbers = Array.Empty<string>(),
        version,
    };

    private static async Task<string?> TitleOf(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("title").GetString();
    }

    /// <summary>One API instance and one signed-in client per role, shared by all tests.</summary>
    public sealed class Sessions : IAsyncLifetime
    {
        public SecurityApiFactory Factory { get; } = new();
        public SessionClient Admin { get; private set; } = null!;
        public SessionClient Analyst { get; private set; } = null!;
        public SessionClient Viewer { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            if (!SecurityApiFactory.IsConfigured)
            {
                return;
            }

            Admin = await Factory.SignInAsync("admin@forensic.local");
            Analyst = await Factory.SignInAsync("analyst@forensic.local");
            Viewer = await Factory.SignInAsync("viewer@forensic.local");
        }

        public Task DisposeAsync() => Factory.DisposeAsync();
    }
}
