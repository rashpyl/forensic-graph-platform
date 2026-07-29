using ForensicGraph.Application.Common;
using ForensicGraph.Application.CrimeEvents;
using ForensicGraph.Application.Persons;
using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Tests.Application;

public class CrimeEventServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 27, 10, 30, 0, DateTimeKind.Utc);

    private static CrimeEventWriteDto ValidWrite(DateTime? occurredAt = null) => new()
    {
        Title = "Break-in",
        Description = "Rear door forced.",
        Address = "Main St. 42",
        OccurredAt = occurredAt ?? Now.AddDays(-1),
        Severity = 3,
        Latitude = 50.0619,
        Longitude = 19.9368,
    };

    private static Person SamplePerson(TestTimeProvider time)
    {
        return Person.Create(
            id: Guid.NewGuid(),
            firstName: "Anna",
            lastName: "Nováková",
            citizenships: new[] { "CZ" },
            passportNumbers: new[] { "P123" },
            phone: null,
            physicalDescription: null,
            nowUtc: time.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task CreateAsync_sets_created_and_updated_to_current_utc()
    {
        var (service, repo, _, _) = BuildService();

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
        var (service, repo, _, _) = BuildService();

        var dto = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        Assert.True(repo.Store.ContainsKey(dto.Id));
    }

    [Fact]
    public async Task GetAsync_returns_dto_for_existing_id()
    {
        var (service, _, _, _) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        var fetched = await service.GetAsync(created.Id, CancellationToken.None);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Empty(fetched.Persons);
        Assert.Empty(fetched.Links);
    }

    [Fact]
    public async Task GetAsync_throws_NotFound_for_unknown_id()
    {
        var (service, _, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_bumps_UpdatedAt_but_keeps_CreatedAt()
    {
        var (service, _, _, time) = BuildService();
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
        var (service, _, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), ValidWrite(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_removes_entity_and_calls_SaveChanges()
    {
        var (service, repo, _, _) = BuildService();
        var created = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await service.DeleteAsync(created.Id, CancellationToken.None);

        Assert.False(repo.Store.ContainsKey(created.Id));
        Assert.Contains($"Remove:{created.Id}", repo.Operations);
        Assert.Equal(2, repo.Operations.Count(op => op == "SaveChanges"));
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFound_for_unknown_id()
    {
        var (service, _, _, _) = BuildService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_applies_default_page_and_page_size()
    {
        var (service, _, _, _) = BuildService();
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
        var (service, _, _, _) = BuildService();

        var result = await service.ListAsync(
            new CrimeEventListQuery { PageSize = 1000 },
            CancellationToken.None);

        Assert.Equal(CrimeEventListQuery.MaxPageSize, result.PageSize);
    }

    // ---- AssignPersonAsync -------------------------------------------------

    [Fact]
    public async Task AssignPersonAsync_persists_the_join_row()
    {
        var (service, repo, personRepo, time) = BuildService();
        var evt = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var person = SamplePerson(time);
        personRepo.Store[person.Id] = person;

        await service.AssignPersonAsync(evt.Id, person.Id, EventRole.Victim, CancellationToken.None);

        Assert.Contains(repo.PersonAssignments,
            a => a.CrimeEventId == evt.Id && a.PersonId == person.Id && a.Role == EventRole.Victim);
    }

    [Fact]
    public async Task AssignPersonAsync_throws_NotFound_when_event_missing()
    {
        var (service, _, personRepo, time) = BuildService();
        var person = SamplePerson(time);
        personRepo.Store[person.Id] = person;

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.AssignPersonAsync(Guid.NewGuid(), person.Id, EventRole.Witness, CancellationToken.None));
    }

    [Fact]
    public async Task AssignPersonAsync_throws_NotFound_when_person_missing()
    {
        var (service, _, _, _) = BuildService();
        var evt = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.AssignPersonAsync(evt.Id, Guid.NewGuid(), EventRole.Victim, CancellationToken.None));
    }

    [Fact]
    public async Task AssignPersonAsync_throws_Conflict_on_duplicate_role()
    {
        var (service, _, personRepo, time) = BuildService();
        var evt = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var person = SamplePerson(time);
        personRepo.Store[person.Id] = person;

        await service.AssignPersonAsync(evt.Id, person.Id, EventRole.Suspect, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AssignPersonAsync(evt.Id, person.Id, EventRole.Suspect, CancellationToken.None));
    }

    [Fact]
    public async Task AssignPersonAsync_allows_same_person_different_role()
    {
        var (service, repo, personRepo, time) = BuildService();
        var evt = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var person = SamplePerson(time);
        personRepo.Store[person.Id] = person;

        await service.AssignPersonAsync(evt.Id, person.Id, EventRole.Suspect, CancellationToken.None);
        await service.AssignPersonAsync(evt.Id, person.Id, EventRole.Witness, CancellationToken.None);

        Assert.Equal(2, repo.PersonAssignments.Count);
    }

    // ---- UnassignPersonAsync ----------------------------------------------

    [Fact]
    public async Task UnassignPersonAsync_removes_only_the_specified_role()
    {
        var (service, repo, personRepo, time) = BuildService();
        var evt = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var person = SamplePerson(time);
        personRepo.Store[person.Id] = person;
        await service.AssignPersonAsync(evt.Id, person.Id, EventRole.Suspect, CancellationToken.None);
        await service.AssignPersonAsync(evt.Id, person.Id, EventRole.Witness, CancellationToken.None);

        await service.UnassignPersonAsync(evt.Id, person.Id, EventRole.Suspect, CancellationToken.None);

        Assert.DoesNotContain(repo.PersonAssignments, a => a.Role == EventRole.Suspect);
        Assert.Contains(repo.PersonAssignments, a => a.Role == EventRole.Witness);
    }

    [Fact]
    public async Task UnassignPersonAsync_throws_NotFound_when_assignment_missing()
    {
        var (service, _, _, _) = BuildService();
        var evt = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UnassignPersonAsync(evt.Id, Guid.NewGuid(), EventRole.Victim, CancellationToken.None));
    }

    // ---- LinkEventAsync ----------------------------------------------------

    [Fact]
    public async Task LinkEventAsync_persists_the_link()
    {
        var (service, repo, _, _) = BuildService();
        var e1 = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var e2 = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await service.LinkEventAsync(e1.Id, e2.Id, "same car", CancellationToken.None);

        Assert.Contains(repo.Links, l => l.FromEventId == e1.Id && l.ToEventId == e2.Id);
    }

    [Fact]
    public async Task LinkEventAsync_throws_Conflict_on_self_link()
    {
        var (service, _, _, _) = BuildService();
        var e = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.LinkEventAsync(e.Id, e.Id, null, CancellationToken.None));
    }

    [Fact]
    public async Task LinkEventAsync_throws_NotFound_when_source_missing()
    {
        var (service, _, _, _) = BuildService();
        var e = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.LinkEventAsync(Guid.NewGuid(), e.Id, null, CancellationToken.None));
    }

    [Fact]
    public async Task LinkEventAsync_throws_NotFound_when_target_missing()
    {
        var (service, _, _, _) = BuildService();
        var e = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.LinkEventAsync(e.Id, Guid.NewGuid(), null, CancellationToken.None));
    }

    [Fact]
    public async Task LinkEventAsync_throws_Conflict_on_duplicate()
    {
        var (service, _, _, _) = BuildService();
        var e1 = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var e2 = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        await service.LinkEventAsync(e1.Id, e2.Id, null, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.LinkEventAsync(e1.Id, e2.Id, null, CancellationToken.None));
    }

    // ---- UnlinkEventAsync -------------------------------------------------

    [Fact]
    public async Task UnlinkEventAsync_removes_the_link()
    {
        var (service, repo, _, _) = BuildService();
        var e1 = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var e2 = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        await service.LinkEventAsync(e1.Id, e2.Id, null, CancellationToken.None);

        await service.UnlinkEventAsync(e1.Id, e2.Id, CancellationToken.None);

        Assert.DoesNotContain(repo.Links, l => l.FromEventId == e1.Id && l.ToEventId == e2.Id);
    }

    [Fact]
    public async Task UnlinkEventAsync_throws_NotFound_when_link_missing()
    {
        var (service, _, _, _) = BuildService();
        var e1 = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var e2 = await service.CreateAsync(ValidWrite(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UnlinkEventAsync(e1.Id, e2.Id, CancellationToken.None));
    }

    // ---- Detail projection -------------------------------------------------

    [Fact]
    public async Task GetAsync_populates_persons_and_links()
    {
        var (service, _, personRepo, time) = BuildService();
        var e1 = await service.CreateAsync(ValidWrite(), CancellationToken.None);
        var e2 = await service.CreateAsync(ValidWrite() with { Title = "Chase down alley" }, CancellationToken.None);
        var person = SamplePerson(time);
        personRepo.Store[person.Id] = person;
        await service.AssignPersonAsync(e1.Id, person.Id, EventRole.Witness, CancellationToken.None);
        await service.LinkEventAsync(e1.Id, e2.Id, "same suspect", CancellationToken.None);

        var detail = await service.GetAsync(e1.Id, CancellationToken.None);

        Assert.Single(detail.Persons);
        Assert.Equal("Anna", detail.Persons[0].FirstName);
        Assert.Equal(EventRole.Witness, detail.Persons[0].Role);
        Assert.Single(detail.Links);
        Assert.Equal(e2.Id, detail.Links[0].ToEventId);
        Assert.Equal("Chase down alley", detail.Links[0].ToEventTitle);
        Assert.Equal("same suspect", detail.Links[0].Note);
    }

    // ---- Test infrastructure ----------------------------------------------

    private static (CrimeEventService Service,
                    StubCrimeEventRepository Repo,
                    StubPersonRepository PersonRepo,
                    TestTimeProvider Time)
        BuildService()
    {
        var time = new TestTimeProvider(Now);
        var personRepo = new StubPersonRepository();
        var repo = new StubCrimeEventRepository();
        var service = new CrimeEventService(repo, personRepo, time);
        return (service, repo, personRepo, time);
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
        public List<EventPerson> PersonAssignments { get; } = new();
        public List<EventLink> Links { get; } = new();
        public List<string> Operations { get; } = new();

        public Task<CrimeEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Store.TryGetValue(id, out var entity);
            return Task.FromResult(entity);
        }

        public Task<CrimeEvent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!Store.TryGetValue(id, out var entity))
            {
                return Task.FromResult<CrimeEvent?>(null);
            }

            // The stub uses the aggregate's private navigation lists via reflection to
            // synchronise with our test-tracked join collections, so GetAsync can
            // project persons and links realistically.
            SyncNavigations(entity);
            return Task.FromResult<CrimeEvent?>(entity);
        }

        public Task<(IReadOnlyList<CrimeEvent> Items, int TotalCount)> ListAsync(
            CrimeEventListQuery query,
            CancellationToken cancellationToken)
        {
            var list = Store.Values.ToList();
            return Task.FromResult<(IReadOnlyList<CrimeEvent>, int)>((list, list.Count));
        }

        public Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken)
        {
            var set = new HashSet<Guid>(ids);
            IReadOnlyDictionary<Guid, string> dict = Store.Values
                .Where(e => set.Contains(e.Id))
                .ToDictionary(e => e.Id, e => e.Title);
            return Task.FromResult(dict);
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

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Store.ContainsKey(id));
        }

        public Task AddPersonAssignmentAsync(EventPerson assignment, CancellationToken cancellationToken)
        {
            PersonAssignments.Add(assignment);
            Operations.Add($"Assign:{assignment.CrimeEventId}:{assignment.PersonId}:{assignment.Role}");
            return Task.CompletedTask;
        }

        public Task RemovePersonAssignmentAsync(
            Guid crimeEventId,
            Guid personId,
            EventRole role,
            CancellationToken cancellationToken)
        {
            PersonAssignments.RemoveAll(a =>
                a.CrimeEventId == crimeEventId && a.PersonId == personId && a.Role == role);
            Operations.Add($"Unassign:{crimeEventId}:{personId}:{role}");
            return Task.CompletedTask;
        }

        public Task AddLinkAsync(EventLink link, CancellationToken cancellationToken)
        {
            Links.Add(link);
            Operations.Add($"Link:{link.FromEventId}->{link.ToEventId}");
            return Task.CompletedTask;
        }

        public Task RemoveLinkAsync(Guid fromEventId, Guid toEventId, CancellationToken cancellationToken)
        {
            Links.RemoveAll(l => l.FromEventId == fromEventId && l.ToEventId == toEventId);
            Operations.Add($"Unlink:{fromEventId}->{toEventId}");
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            Operations.Add("SaveChanges");
            return Task.FromResult(1);
        }

        private void SyncNavigations(CrimeEvent entity)
        {
            var personsField = typeof(CrimeEvent).GetField("_persons",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var linksField = typeof(CrimeEvent).GetField("_outgoingLinks",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            var personsList = (List<EventPerson>)personsField.GetValue(entity)!;
            personsList.Clear();
            personsList.AddRange(PersonAssignments.Where(a => a.CrimeEventId == entity.Id));

            var linksList = (List<EventLink>)linksField.GetValue(entity)!;
            linksList.Clear();
            linksList.AddRange(Links.Where(l => l.FromEventId == entity.Id));
        }
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
