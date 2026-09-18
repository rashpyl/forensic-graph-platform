using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Domain.Persons;
using ForensicGraph.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForensicGraph.Infrastructure.Seeding;

/// <summary>
/// Dev-only demo seeder. Populates the database with ~30 crime events across
/// four European cities (Prague, Kyiv, Lviv, Berlin), ~50 persons, plausible
/// person↔event role assignments, and a handful of manually-drawn event links
/// so the map UI has something interesting to render out of the box.
///
/// Idempotent by early-exit: if any <c>CrimeEvent</c> already exists we skip.
/// Uses a fixed <see cref="Random"/> seed so consecutive runs on a fresh DB
/// produce the same data — makes screenshots and demos reproducible.
/// </summary>
public static class DatabaseSeeder
{
    private const int RandomSeed = 20260803;

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForensicGraphDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ForensicGraph.Seeder");

        if (await db.CrimeEvents.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seed skipped: crime_events table is not empty.");
            return;
        }

        var rng = new Random(RandomSeed);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var persons = BuildPersons(rng, nowUtc);
        await db.Persons.AddRangeAsync(persons, cancellationToken);

        var events = BuildEvents(rng, nowUtc);
        await db.CrimeEvents.AddRangeAsync(events, cancellationToken);

        var assignments = BuildAssignments(rng, events, persons, nowUtc);
        await db.EventPersons.AddRangeAsync(assignments, cancellationToken);

        var links = BuildLinks(rng, events, nowUtc);
        await db.EventLinks.AddRangeAsync(links, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Persons} persons, {Events} events, {Assignments} assignments, {Links} links.",
            persons.Count, events.Count, assignments.Count, links.Count);
    }

    private static List<Person> BuildPersons(Random rng, DateTime nowUtc)
    {
        // Deliberately mixed nationalities to exercise the citizenships text[] column.
        string[] firstNames =
        {
            "Adam", "Barbora", "Cyril", "Dana", "Elena", "Filip", "Gabriela", "Hana",
            "Ivan", "Jana", "Karel", "Lukáš", "Marie", "Natálie", "Ondřej", "Petra",
            "Radek", "Sofia", "Tomáš", "Ulyana", "Věra", "Wolfgang", "Xenia", "Yaroslav",
            "Zuzana", "Anastasia", "Bohdan", "Clara", "Denys", "Eva", "Frank", "Greta",
            "Halyna", "Igor", "Julia", "Katarína", "Leon", "Mila", "Nika", "Oleksandr",
            "Pavel", "Renata", "Sergiy", "Tereza", "Uwe", "Viktor", "Wanda", "Yuri",
            "Zdeněk", "Iryna",
        };
        string[] lastNames =
        {
            "Novák", "Svobodová", "Dvořák", "Černý", "Procházka", "Kučera", "Veselý",
            "Horák", "Němec", "Marek", "Pokorný", "Pospíšil", "Hájek", "Král", "Jelínek",
            "Šimek", "Růžička", "Havel", "Beneš", "Fiala", "Melnyk", "Shevchenko",
            "Kovalenko", "Bondar", "Koval", "Boyko", "Tkachenko", "Kravchenko",
            "Oliynyk", "Moroz", "Schmidt", "Müller", "Schneider", "Fischer", "Weber",
            "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann", "Klein", "Wolf",
            "Neumann", "Schwarz", "Zimmermann", "Braun", "Krüger", "Hartmann",
            "Lange", "Werner",
        };
        string[] citizenshipPool = { "CZ", "UA", "DE", "PL", "SK" };

        var count = 50;
        var list = new List<Person>(count);
        for (var i = 0; i < count; i++)
        {
            var first = firstNames[i % firstNames.Length];
            var last = lastNames[rng.Next(lastNames.Length)];
            var citizenships = new[] { citizenshipPool[rng.Next(citizenshipPool.Length)] };
            var passports = rng.NextDouble() < 0.6
                ? new[] { $"P{rng.Next(1_000_000, 9_999_999)}" }
                : Array.Empty<string>();
            var phone = rng.NextDouble() < 0.7
                ? $"+420 {rng.Next(600, 800)} {rng.Next(100, 999)} {rng.Next(100, 999)}"
                : null;
            var physical = rng.NextDouble() < 0.5
                ? $"Approx. {160 + rng.Next(0, 40)} cm, {50 + rng.Next(0, 50)} kg."
                : null;

            list.Add(Person.Create(
                id: Guid.NewGuid(),
                firstName: first,
                lastName: last,
                citizenships: citizenships,
                passportNumbers: passports,
                phone: phone,
                physicalDescription: physical,
                nowUtc: nowUtc));
        }
        return list;
    }

    private static List<CrimeEvent> BuildEvents(Random rng, DateTime nowUtc)
    {
        (string City, double Lat, double Lng, double Radius, string Country)[] cities =
        {
            ("Prague", 50.0755, 14.4378, 0.05, "Czech Republic"),
            ("Kyiv",   50.4501, 30.5234, 0.08, "Ukraine"),
            ("Lviv",   49.8397, 24.0297, 0.05, "Ukraine"),
            ("Berlin", 52.5200, 13.4050, 0.08, "Germany"),
        };
        string[] titlePool =
        {
            "Reported burglary at residential address",
            "Assault outside night club",
            "Stolen vehicle recovered",
            "Suspicious package on public transport",
            "Fraud investigation opened",
            "Vandalism reported at storefront",
            "Missing person case",
            "Robbery at convenience store",
            "Drug possession arrest",
            "Domestic disturbance call",
            "Pickpocketing incident on tram",
            "Illegal graffiti at public monument",
            "Bar fight — multiple injuries",
            "Bicycle theft from station rack",
            "Cyber-fraud victim complaint",
            "Illegal parking dispute escalated",
        };
        string[] descPool =
        {
            "Officers dispatched to the scene; report in progress.",
            "Preliminary witness statements collected.",
            "CCTV footage requested from adjacent premises.",
            "Case forwarded to detective unit for follow-up.",
            "No suspects apprehended at time of writing.",
            "One person detained pending questioning.",
            "Damaged property estimated at approximately €1,500.",
            "Incident linked to earlier report from the same block.",
        };

        var count = 30;
        var list = new List<CrimeEvent>(count);
        for (var i = 0; i < count; i++)
        {
            var city = cities[i % cities.Length];
            var lat = city.Lat + (rng.NextDouble() - 0.5) * city.Radius;
            var lng = city.Lng + (rng.NextDouble() - 0.5) * city.Radius;
            var title = titlePool[rng.Next(titlePool.Length)];
            var description = descPool[rng.Next(descPool.Length)];
            var address = $"{rng.Next(1, 200)} Main St, {city.City}, {city.Country}";
            var occurredAt = nowUtc.AddDays(-rng.Next(1, 365)).AddHours(-rng.Next(0, 24));
            var severity = rng.Next(1, 6);

            list.Add(CrimeEvent.Create(
                id: Guid.NewGuid(),
                title: title,
                description: description,
                address: address,
                occurredAt: occurredAt,
                severity: severity,
                latitude: lat,
                longitude: lng,
                nowUtc: nowUtc));
        }
        return list;
    }

    private static List<EventPerson> BuildAssignments(
        Random rng,
        IReadOnlyList<CrimeEvent> events,
        IReadOnlyList<Person> persons,
        DateTime nowUtc)
    {
        var roles = new[]
        {
            EventRole.Victim, EventRole.Suspect, EventRole.Witness,
            EventRole.Perpetrator, EventRole.Reporter, EventRole.Officer,
        };
        var list = new List<EventPerson>();
        foreach (var ev in events)
        {
            var assignmentCount = rng.Next(2, 5);
            var usedKeys = new HashSet<(Guid PersonId, EventRole Role)>();
            for (var i = 0; i < assignmentCount; i++)
            {
                var person = persons[rng.Next(persons.Count)];
                var role = roles[rng.Next(roles.Length)];
                if (!usedKeys.Add((person.Id, role)))
                {
                    continue;
                }
                list.Add(EventPerson.Create(ev.Id, person.Id, role, nowUtc));
            }
        }
        return list;
    }

    private static List<EventLink> BuildLinks(
        Random rng,
        IReadOnlyList<CrimeEvent> events,
        DateTime nowUtc)
    {
        var list = new List<EventLink>();
        var linkCount = 6;
        var used = new HashSet<(Guid, Guid)>();
        for (var i = 0; i < linkCount; i++)
        {
            var from = events[rng.Next(events.Count)];
            var to = events[rng.Next(events.Count)];
            if (from.Id == to.Id || !used.Add((from.Id, to.Id)))
            {
                continue;
            }
            list.Add(EventLink.Create(
                from.Id,
                to.Id,
                note: "Investigator suspects a connection between these events.",
                nowUtc: nowUtc));
        }
        return list;
    }
}
