using ForensicGraph.Application.Common;
using ForensicGraph.Application.Persons;
using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Tests.Application;

public class PersonServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 11, 15, 0, DateTimeKind.Utc);

    private static PersonWriteDto ValidWrite() => new()
    {
        FirstName = "Anna",
        LastName = "Nováková",
        Citizenships = new[] { "CZ" },
        PassportNumbers = new[] { "P123" },
        Phone = "+420111222333",
        PhysicalDescription = "Tall, brown hair.",
    };

    [Fact]
    public async Task CreateAsync_persists_and_stamps_timestamps()
    {
        var (service, repo, _) = BuildService();

        var dto = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal(Now, dto.CreatedAt);
        Assert.Equal(Now, dto.UpdatedAt);
        Assert.True(repo.Store.ContainsKey(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_copies_citizenships_and_passports()
    {
        var (service, _, _) = BuildService();

        var dto = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        Assert.Equal(new[] { "CZ" }, dto.Citizenships);
        Assert.Equal(new[] { "P123" }, dto.PassportNumbers);
    }

    [Fact]
    public async Task GetAsync_returns_persisted_person()
    {
        var (service, _, _) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        var fetched = await service.GetAsync(created.Id, CancellationToken.None);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Anna", fetched.FirstName);
    }

    [Fact]
    public async Task GetAsync_throws_NotFound_for_unknown_id()
    {
        var (service, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_replaces_scalar_and_collection_fields()
    {
        var (service, _, time) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        time.Advance(TimeSpan.FromHours(1));
        var updated = await service.UpdateAsync(
            created.Id,
            ValidWrite() with
            {
                LastName = "Novak",
                Citizenships = new[] { "CZ", "SK" },
            },
            CancellationToken.None);

        Assert.Equal("Novak", updated.LastName);
        Assert.Equal(new[] { "CZ", "SK" }, updated.Citizenships);
        Assert.Equal(Now.AddHours(1), updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFound_for_unknown_id()
    {
        var (service, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), ValidWrite(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves()
    {
        var (service, repo, _) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await service.DeleteAsync(created.Id, CancellationToken.None);

        Assert.False(repo.Store.ContainsKey(created.Id));
    }

    [Fact]
    public async Task ListAsync_normalises_page_size()
    {
        var (service, _, _) = BuildService();
        await service.CreateAsync(ValidWrite(), CancellationToken.None);

        var result = await service.ListAsync(
            new PersonListQuery { PageSize = 5000 },
            CancellationToken.None);

        Assert.Equal(PersonListQuery.MaxPageSize, result.PageSize);
    }

    private static (PersonService Service, StubPersonRepository Repo, TestTimeProvider Time)
        BuildService()
    {
        var time = new TestTimeProvider(Now);
        var repo = new StubPersonRepository();
        var service = new PersonService(repo, time);
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

    private sealed class StubPersonRepository : IPersonRepository
    {
        public Dictionary<Guid, Person> Store { get; } = new();

        public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Store.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public Task<IReadOnlyList<Person>> GetManyAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken)
        {
            var set = new HashSet<Guid>(ids);
            IReadOnlyList<Person> result = Store.Values.Where(p => set.Contains(p.Id)).ToList();
            return Task.FromResult(result);
        }

        public Task<(IReadOnlyList<Person> Items, int TotalCount)> ListAsync(
            PersonListQuery query,
            CancellationToken cancellationToken)
        {
            var list = Store.Values.ToList();
            return Task.FromResult<(IReadOnlyList<Person>, int)>((list, list.Count));
        }

        public Task AddAsync(Person entity, CancellationToken cancellationToken)
        {
            Store[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Person entity, CancellationToken cancellationToken)
        {
            Store[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Person entity, CancellationToken cancellationToken)
        {
            Store.Remove(entity.Id);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
