# Developer Guide

**This document covers:** repository layout, architecture, coding
conventions, and step-by-step recipes for the most common changes.

If you are looking for how to *install* the app, see [`../README.md`](../README.md).
If you are looking for how to *use* the app, see [`USER_GUIDE.md`](./USER_GUIDE.md).

---

## 1. Repository layout

```
forensic-graph-platform/
├── backend/
│   └── src/
│       ├── ForensicGraph.Domain/         Entities, value objects, domain rules
│       ├── ForensicGraph.Application/    Use cases, DTOs, service abstractions
│       ├── ForensicGraph.Infrastructure/ EF Core DbContext, migrations, seeder
│       └── ForensicGraph.Api/            ASP.NET Core controllers, DI wiring
├── backend/tests/
│   └── ForensicGraph.Tests/              xUnit tests
├── frontend/
│   └── src/
│       ├── views/                        Route-level pages
│       ├── components/                   Reusable pieces (dialogs, panels)
│       ├── stores/                       Pinia stores (one per domain area)
│       ├── services/                     HTTP clients and error helpers
│       ├── composables/                  Cross-cutting reactive helpers
│       ├── types/                        TypeScript mirrors of backend DTOs
│       └── router/                       Vue Router configuration
├── docs/                                  This directory
├── docker-compose.yml                     Local PostgreSQL container
└── README.md
```

---

## 2. Backend architecture

The backend follows **clean architecture** with a strict dependency
direction: outer layers reference inner layers, never the reverse.

```
Api ─────► Application ─────► Domain
 │             │
 └─► Infrastructure ─────► Application ─────► Domain
```

### 2.1 Domain
Pure C#. Entities: `Person`, `CrimeEvent`,
`EventPerson` (join with role), `EventLink`. Value objects for coordinates.
Domain exceptions: `NotFoundException`, `ConflictException`,
`DomainValidationException`.

**Guideline.** Never leak EF Core types into `Domain`.

### 2.2 Application
Use-case services (`ICrimeEventService`, `IPersonService`) and their
implementations, plus request/response DTOs. FluentValidation validators
live here.

**Guideline.** Application talks to the database via repository interfaces
(`ICrimeEventRepository`, `IPersonRepository`) declared here and
implemented in Infrastructure.

### 2.3 Infrastructure
EF Core `ForensicGraphDbContext`, entity configurations, migrations,
repository implementations, and the dev-only `DatabaseSeeder`.

**Guideline.** Any new entity needs (a) a `Domain` class, (b) an
`IEntityTypeConfiguration<T>` under `Infrastructure/Persistence/Configurations`,
(c) a repository interface in `Application` + implementation here, and
(d) a migration.

### 2.4 Api
Controllers, `Program.cs`, DI wiring, `GlobalExceptionHandler`, JSON
converters. This is the only layer that knows about HTTP.

**Guideline.** Controllers only:
- Validate input via `[ApiController]` + FluentValidation.
- Delegate to an Application service.
- Return `Ok(dto)` / `CreatedAtAction(...)` / `NoContent()`.
Domain exceptions bubble up and `GlobalExceptionHandler` maps them:

| Exception                    | HTTP status |
|------------------------------|-------------|
| `NotFoundException`          | 404         |
| `ConflictException`          | 409         |
| `DomainValidationException`  | 400         |
| Anything else                | 500         |

---

## 3. Key backend conventions

### 3.1 Wire formats
- **Dates** are ISO-8601 UTC on the wire, enforced by
  `Iso8601UtcDateTimeConverter` (`ForensicGraph.Api/Serialization/`).
  Never accept "naive" datetimes.
- **Enums** are serialised camelCase strings via
  `JsonStringEnumConverter(CamelCase)`. `EventRole.Victim` becomes
  `"victim"` on the wire.
- **Paged results** always use the shape
  `{ items, totalCount, page, pageSize }`. Do *not* invent `total` or
  `count`.

### 3.2 Seed data
`ForensicGraph.Infrastructure/Seeding/DatabaseSeeder.cs` runs during
`Program.cs` startup only when `env.IsDevelopment()`. It:
1. Applies pending EF migrations.
2. Bails out early if `crime_events` is non-empty (idempotent).
3. Uses a fixed random seed (`20260803`) so every developer sees the same
   demo world.

### 3.3 Sub-resource routes
Composite-key deletions pass the extra key as a query parameter, not a
segment — the URL should read naturally when eyeballing logs:

```
DELETE /api/events/{eventId}/persons/{personId}?role=Victim
DELETE /api/events/{eventId}/links/{toEventId}
```

### 3.4 Configuration
The Postgres connection string is intentionally required. If it is
missing the API fails to start rather than falling back to a "helpful"
default that would silently hide a misconfiguration.

Store it in **user secrets**, never in `appsettings.json`:

```bash
cd backend/src/ForensicGraph.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=…"
```

### 3.5 CORS
`AddForensicGraphCors` allows exactly one origin: `http://localhost:5173`.
This is deliberate — for MVP we don't need multiple origins and refusing
`127.0.0.1` catches the class of bugs where developers mix the two
hostnames and get confusing preflight failures.

---

## 4. Frontend architecture

Vue 3 + `<script setup>` TypeScript, Pinia for state, Vue Router for
navigation, Leaflet for the map, Axios for HTTP.

### 4.1 Layers
- **`types/api.ts`** is the single source of truth for the wire shapes.
  Components never invent their own event/person types.
- **`services/`** wraps `axios` per resource (`eventsApi`, `personsApi`).
  This layer knows about URLs and status codes.
- **`stores/`** owns state and calls services. One store per domain area
  (`events`, `persons`, plus `ui` for cross-cutting UI flags).
- **`composables/`** for reactive helpers reusable across views
  (`useSeverity`, `useDateTime`).
- **`components/`** are dumb UIs bound to stores; they don't call axios
  directly.
- **`views/`** are route-level entry points that compose components.

### 4.2 State sync pattern
Every mutation on `stores/events.ts` that changes the currently-selected
event calls `refreshSelected()` afterwards, so `EventDetailPanel`'s
persons and links stay accurate without prop-drilling.

### 4.3 Formatting
- Dates displayed in the UI use the `useDateTime` composable which
  formats to European `dd.MM.yyyy HH:mm` in local time.
- Datetimes sent to the API are converted local → ISO-8601 UTC by the
  same composable. Never `.toISOString()` a naïve string.
- Severity 1–5 maps to a fixed green→red palette in `useSeverity`.

### 4.4 API base URL
`services/apiClient.ts` reads `import.meta.env.VITE_API_BASE_URL`,
sourced from `.env.development`. The build fails loudly if the variable
is missing.

---

## 5. Recipes

### 5.1 Add a new domain entity end-to-end

1. **Domain**: add the class under `ForensicGraph.Domain/…`. Keep it
   framework-free.
2. **Application**: create `EntityDto`, `EntityWriteDto`, an
   `IEntityService` + implementation, an `IEntityRepository`, and a
   FluentValidation validator.
3. **Infrastructure**:
   - Add a `DbSet<Entity>` to `ForensicGraphDbContext`.
   - Add an `IEntityTypeConfiguration<Entity>` under
     `Infrastructure/Persistence/Configurations`.
   - Implement `EntityRepository : IEntityRepository`.
   - Create a migration:
     ```bash
     cd backend/src/ForensicGraph.Infrastructure
     dotnet ef migrations add AddEntity --startup-project ../ForensicGraph.Api
     ```
4. **Api**:
   - Add `EntitiesController` under `Controllers/`.
   - Register the service + repository in `Program.cs` via the DI setup.
5. **Frontend**:
   - Extend `types/api.ts` with `EntityDto` and `EntityWriteDto`.
   - Create `services/entityApi.ts`.
   - Create `stores/entity.ts`.
   - Build the view / dialog components.
   - Add the route in `router/index.ts` if the entity gets its own page.

### 5.2 Add a new event field

1. Add the property to `CrimeEvent` (Domain) and the migration.
2. Extend `CrimeEventDto` / `CrimeEventWriteDto`.
3. Update the FluentValidation validator.
4. Update the mapping in `CrimeEventService`.
5. Mirror the property in `frontend/src/types/api.ts`.
6. Surface it in `NewEventDialog.vue` and `EventDetailPanel.vue`.

### 5.3 Change severity palette

Everything is in `frontend/src/composables/useSeverity.ts` — that
composable feeds both the map markers and the New Event dialog swatch.
No CSS changes needed elsewhere.

### 5.4 Reseed the database

The seeder short-circuits when data exists, so reseed by dropping the
tables:

```bash
docker compose exec postgres psql -U forensic -d forensic_graph -c "TRUNCATE crime_events, persons CASCADE;"
```

Then restart the API.

---

## 6. Testing

### 6.1 Backend

```bash
dotnet test backend/src/ForensicGraph.sln
```

xUnit is the test framework. Application services use in-memory
substitutes for repositories; the Infrastructure layer uses the
Testcontainers-backed Postgres for integration tests where relevant.

### 6.2 Frontend

There are no unit tests for the MVP. The frontend is validated by the
typechecked production build:

```bash
cd frontend && npm run build
```

`vue-tsc` runs during the build, so any type error fails CI.

---

## 7. API reference (Doxygen)

The backend ships with a Doxygen configuration that renders the XML
`/// <summary>` comments (methods, classes, DTOs, controllers) into a
browsable HTML site. Use it as a quick reference for individual types
that this guide does not cover in prose.

### 7.1 Prerequisites

Install Doxygen once per machine:

```bash
# macOS
brew install doxygen

# Debian / Ubuntu
sudo apt-get install doxygen

# Windows
winget install DimitriVanHeesch.Doxygen
```

Optional (enables class-diagram and dependency-graph output):

```bash
brew install graphviz            # macOS
sudo apt-get install graphviz    # Debian / Ubuntu
```

After installing Graphviz, flip `HAVE_DOT = YES` in `Doxyfile` at the
repo root, then regenerate.

### 7.2 Generate the docs

From the repository root:

```bash
doxygen Doxyfile
```

Output is written to `docs/api/html/`. Any warnings land in
`docs/api/doxygen-warnings.log`.

The `docs/api/` folder is generated output and is not committed —
regenerate whenever the XML doc comments change.

### 7.3 Open the docs

```bash
# macOS
open docs/api/html/index.html

# Linux
xdg-open docs/api/html/index.html

# Windows (PowerShell)
Start-Process docs/api/html/index.html
```

The landing page is this Developer Guide plus the top-level README.
Use the left-hand tree or the search box (top-right) to jump into
individual namespaces:

- `ForensicGraph.Domain` — aggregate roots and value objects
- `ForensicGraph.Application` — services, DTOs, repository contracts
- `ForensicGraph.Infrastructure` — EF Core context, configurations, repositories
- `ForensicGraph.Api` — controllers, request models, middleware

### 7.4 Serve the docs locally (optional)

If you prefer a URL over `file://`, any static server works:

```bash
cd docs/api/html
python3 -m http.server 8000
# open http://localhost:8000
```

---

## 8. Git conventions

- Trunk is `develop`. `main` is protected and receives releases only.
- Every feature ships as `feature/<slug>` merged with a PR into
  `develop`.
- Commits are backdated to the summer/autumn of 2026 to reconstruct the
  development timeline for the thesis defence. The pattern is:

  ```bash
  GIT_AUTHOR_DATE="2026-08-19T20:11:37+0200" \
  GIT_COMMITTER_DATE="2026-08-19T20:11:37+0200" \
  git commit -m "…"
  ```

- OpenSpec change proposals live under `openspec/changes/`. The current
  change tracks all seven implementation chunks:
  `openspec/changes/investigation-map-mvp/`.

---

## 9. Known limitations (intentional for MVP)

- **No authentication.** The API is open on `localhost` only. Adding
  auth is a diploma-scope extension, not annual-project scope.
- **No refresh tokens / audit logs / row-level security.** Same reason.
- **No frontend tests.** Only the type-checked build gates the frontend.
- **No optimistic concurrency.** Two simultaneous edits to the same
  event overwrite each other last-write-wins.
- **CORS is single-origin.** Only `http://localhost:5173` is allowed.

Any of these will be lifted later.
