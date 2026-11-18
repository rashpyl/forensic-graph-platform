# Forensic Graph Platform

Graph-based analytical platform for visualizing and analyzing forensic investigation data —
entities (events, persons, locations), their relationships, and network-level statistics.

## Stack
- Frontend: Vue 3, Pinia, Vue Router
- Backend: ASP.NET Core Web API
- Database: PostgreSQL

## Scope
This repository currently covers the **annual project** scope:
- Entity management (CRUD)
- Graph visualization of local event relationships
- Map visualization with filtering
- Basic statistical module

## Setup

The app has three moving parts: **PostgreSQL**, the **ASP.NET Core API**, and the **Vue 3 client**.
Bring them up in that order.

### Prerequisites
- .NET SDK **9.0**
- Node.js **20+** and npm
- Docker (for PostgreSQL) — or a local Postgres instance on port `5433`

### 1. Start PostgreSQL

The repo ships a `docker-compose.yml` at the root that boots a container named
`forensic-graph-postgres` exposing host port **5433** (mapped to the container's 5432 so
it does not clash with any local Postgres you may already run).

```bash
docker compose up -d postgres
```

The container starts with database `forensic_graph`, user `forensic`, password `forensic`.

### 2. Configure the API connection string

The backend intentionally fails fast if the connection string is missing — no fallback default.
Set it as a user-secret so it is never committed:

```bash
cd backend/src/ForensicGraph.Api
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5433;Database=forensic_graph;Username=forensic;Password=forensic"
```

### 3. Run the API

```bash
cd backend/src/ForensicGraph.Api
dotnet run
```

The API listens on **http://localhost:8080**. On first start in Development mode the
`DatabaseSeeder` applies EF migrations and populates ~50 persons and ~30 events across
Prague, Kyiv, Lviv and Berlin. Restarting the API is a no-op — the seeder short-circuits
when the `crime_events` table is non-empty.

It also creates one demo account per role, all with the password from
`Seed:DemoPassword` in `appsettings.Development.json` (`forensic-demo-2026`):

| E-mail | Role | Can |
|---|---|---|
| `admin@forensic.local` | Admin | everything, plus user management and the audit log |
| `analyst@forensic.local` | Analyst | create, edit and delete data; sees passport numbers and phones |
| `viewer@forensic.local` | Viewer | read only; passport numbers and phones are masked |

These accounts exist only in Development. Outside it, create the first administrator by
setting `Bootstrap:AdminEmail` and `Bootstrap:AdminPassword` (user-secrets or environment
variables) before the first start; further accounts are created in the app under **Users**.

Every endpoint except `/healthz`, `/api/auth/login` and `/api/auth/csrf` requires a signed-in
user. Swagger UI (<http://localhost:8080/swagger>) therefore shows the API, but calling it
needs a session cookie and an antiforgery token — the app is the easier way to try it.

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

The Vite dev server serves the app at **http://localhost:5173** and forwards `/api` to the
backend on port 8080 (see `vite.config.ts`), so the browser talks to a single origin and the
session cookie stays first-party. Sign in with one of the demo accounts above.

### 5. What to click
- Sign in. Analyst or Admin to change data, Viewer to see the read-only experience.
- Map opens at `/`. Zoom / pan to see clustered severity-coloured markers.
- **+ New Event** → click a spot on the map → fill the dialog.
- Click any marker to open the right-side detail panel (assign persons, link related events, delete).
- **Persons** in the navbar opens the searchable person catalogue.
- **New Person** opens the create-person dialog from anywhere.
- **About** shows a short summary of the project.
- As Admin: **Users** manages accounts and roles, **Audit log** shows who changed what and when.

### Documentation
- [`docs/USER_GUIDE.md`](docs/USER_GUIDE.md) — using the application.
- [`docs/DEVELOPER_GUIDE.md`](docs/DEVELOPER_GUIDE.md) — architecture, conventions and the
  security model (section 3.6).

### Tests

```bash
cd backend
dotnet test
```

The security integration tests (sign-in, roles, CSRF, masking, edit conflicts, audit log) start
the real API against a temporary PostgreSQL database. They are skipped unless
`FG_TEST_POSTGRES` points at a server where the user may create databases, for example the
docker-compose container:

```bash
FG_TEST_POSTGRES="Host=localhost;Port=5433;Username=forensic;Password=forensic" dotnet test
```

Each run creates its own `fg_test_…` database and drops it afterwards.

Frontend has a type-checked production build:

```bash
cd frontend
npm run build
```