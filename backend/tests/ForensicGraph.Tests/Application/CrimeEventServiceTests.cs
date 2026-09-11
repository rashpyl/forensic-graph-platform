using ForensicGraph.Application.Common;
using ForensicGraph.Application.CrimeEvents;
using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Tests.Application;

public class CrimeEventServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 27, 10, 30, 0, DateTimeKind.Utc);

    private static CrimeEventWriteDto ValidWrite(DateTime? occurredAt = null) => new()
    {
        Title = "Break-in",
        Description = "Rear door forced.",
        OccurredAt = occurredAt ?? Now.AddDays(-1),
        Severity = 3,
        Latitude = 50.0619,
        Longitude = 19.9368,
    };

    [Fact]
    public async Task CreateAsync_sets_created_and_updated_to_current_utc()
    {
        var (service, repo, _) = BuildService();

        var dto = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        Assert.Equal(Now, dto.CreatedAt);
        Assert.Equal(Now, dto.UpdatedAt);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Contains($"Add:{dto.Id}", repo.Operations);
        Assert.Contains("SaveChanges", repo.Operations);
    }

    [Fact]
    public async Task CreateAsync_persists_entity_via_repository()
    {
        var (service, repo, _) = BuildService();

        var dto = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        Assert.True(repo.Store.ContainsKey(dto.Id));
    }

    [Fact]
    public async Task GetAsync_returns_dto_for_existing_id()
    {
        var (service, _, _) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        var fetched = await service.GetAsync(created.Id, CancellationToken.None);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
    }

    [Fact]
    public async Task GetAsync_throws_NotFound_for_unknown_id()
    {
        var (service, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_bumps_UpdatedAt_but_keeps_CreatedAt()
    {
        var (service, _, time) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        time.Advance(TimeSpan.FromHours(2));
        var updated = await service.UpdateAsync(
            created.Id,
            ValidWrite() with { Title = "Break-in (revised)" },
            CancellationToken.None);

        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.Equal(Now.AddHours(2), updated.UpdatedAt);
        Assert.Equal("Break-in (revised)", updated.Title);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFound_for_unknown_id()
    {
        var (service, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), ValidWrite(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_removes_entity_and_calls_SaveChanges()
    {
        var (service, repo, _) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await service.DeleteAsync(created.Id, CancellationToken.None);

        Assert.False(repo.Store.ContainsKey(created.Id));
        Assert.Contains($"Remove:{created.Id}", repo.Operations);
        Assert.Equal(2, repo.Operations.Count(op => op == "SaveChanges"));
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFound_for_unknown_id()
    {
        var (service, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_applies_default_page_and_page_size()
    {
        var (service, _, _) = BuildService();
        await service.CreateAsync(ValidWrite(), CancellationToken.None);

        var result = await service.ListAsync(new CrimeEventListQuery(), CancellationToken.None);

        Assert.Equal(CrimeEventListQuery.DefaultPage, result.Page);
        Assert.Equal(CrimeEventListQuery.DefaultPageSize, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ListAsync_caps_page_size_at_200()
    {
        var (service, _, _) = BuildService();

        var result = await service.ListAsync(
            new CrimeEventListQuery { PageSize = 1000 },
            CancellationToken.None);

        Assert.Equal(CrimeEventListQuery.MaxPageSize, result.PageSize);
    }

    private static (CrimeEventService Service, StubCrimeEventRepository Repo, TestTimeProvider Time)
        BuildService()
    {
        var time = new TestTimeProvider(Now);
        var repo = new StubCrimeEventRepository();
        var service = new CrimeEventService(repo, time);
        return (service, repo, time);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public TestTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
    }

    private sealed class StubCrimeEventRepository : ICrimeEventRepository
    {
        public Dictionary<Guid, CrimeEvent> Store { get; } = new();

        public List<string> Operations { get; } = new();

        public Task<CrimeEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Store.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public Task<(IReadOnlyList<CrimeEvent> Items, int TotalCount)> ListAsync(
            CrimeEventListQuery query,
            CancellationToken cancellationToken)
        {
            var list = Store.Values.ToList();
            return Task.FromResult<(IReadOnlyList<CrimeEvent>, int)>((list, list.Count));
        }

        public Task AddAsync(CrimeEvent entity, CancellationToken cancellationToken)
        {
            Store[entity.Id] = entity;
            Operations.Add($"Add:{entity.Id}");
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CrimeEvent entity, CancellationToken cancellationToken)
        {
            Store[entity.Id] = entity;
            Operations.Add($"Update:{entity.Id}");
            return Task.CompletedTask;
        }

        public Task RemoveAsync(CrimeEvent entity, CancellationToken cancellationToken)
        {
            Store.Remove(entity.Id);
            Operations.Add($"Remove:{entity.Id}");
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Operations.Add("SaveChanges");
            return Task.FromResult(1);
        }
    }
}
