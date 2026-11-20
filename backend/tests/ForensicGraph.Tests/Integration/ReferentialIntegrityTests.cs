using Npgsql;

namespace ForensicGraph.Tests.Integration;

/// <summary>
/// Checks that the database itself enforces the relationships between persons,
/// events and links, independently of the application code.
/// </summary>
public sealed class ReferentialIntegrityTests : IClassFixture<SecurityApiFactory>
{
    private const string ForeignKeyViolation = "23503";

    private readonly SecurityApiFactory _factory;

    public ReferentialIntegrityTests(SecurityApiFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task Rows_pointing_to_missing_person_or_event_are_rejected()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        await using var conn = await OpenAsync();
        var eventId = await InsertEventAsync(conn);

        var orphanAssignment = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(conn,
            "INSERT INTO event_persons (crime_event_id, person_id, role, created_at) VALUES (@e, @missing, 1, now())",
            ("e", eventId), ("missing", Guid.NewGuid())));
        var orphanLink = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(conn,
            "INSERT INTO event_links (from_event_id, to_event_id, created_at) VALUES (@e, @missing, now())",
            ("e", eventId), ("missing", Guid.NewGuid())));

        Assert.Equal(ForeignKeyViolation, orphanAssignment.SqlState);
        Assert.Equal(ForeignKeyViolation, orphanLink.SqlState);
    }

    [SkippableFact]
    public async Task Deleting_person_or_event_removes_dependent_rows()
    {
        Skip.IfNot(SecurityApiFactory.IsConfigured, "FG_TEST_POSTGRES not set");
        await using var conn = await OpenAsync();
        var from = await InsertEventAsync(conn);
        var to = await InsertEventAsync(conn);
        var personId = Guid.NewGuid();
        await ExecuteAsync(conn,
            "INSERT INTO persons (id, first_name, last_name, citizenships, passport_numbers, created_at, updated_at) " +
            "VALUES (@p, 'Test', 'Person', '{}', '{}', now(), now())",
            ("p", personId));
        await ExecuteAsync(conn,
            "INSERT INTO event_persons (crime_event_id, person_id, role, created_at) VALUES (@e, @p, 1, now())",
            ("e", from), ("p", personId));
        await ExecuteAsync(conn,
            "INSERT INTO event_links (from_event_id, to_event_id, created_at) VALUES (@f, @t, now())",
            ("f", from), ("t", to));

        await ExecuteAsync(conn, "DELETE FROM persons WHERE id = @p", ("p", personId));
        Assert.Equal(0L, await CountAsync(conn, "event_persons", "person_id", personId));

        await ExecuteAsync(conn, "DELETE FROM crime_events WHERE id = @t", ("t", to));
        Assert.Equal(0L, await CountAsync(conn, "event_links", "to_event_id", to));
    }

    private async Task<NpgsqlConnection> OpenAsync()
    {
        // Creating a client starts the host, which creates and migrates the test database.
        _factory.CreateClient().Dispose();
        var conn = new NpgsqlConnection(_factory.DatabaseConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static async Task<Guid> InsertEventAsync(NpgsqlConnection conn)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(conn,
            "INSERT INTO crime_events (id, title, occurred_at, severity, created_at, updated_at) " +
            "VALUES (@id, 'Test event', now(), 1, now(), now())",
            ("id", id));
        return id;
    }

    private static async Task ExecuteAsync(NpgsqlConnection conn, string sql, params (string Name, object Value)[] args)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in args)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountAsync(NpgsqlConnection conn, string table, string column, Guid value)
    {
        await using var cmd = new NpgsqlCommand($"SELECT count(*) FROM {table} WHERE {column} = @v", conn);
        cmd.Parameters.AddWithValue("v", value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}
