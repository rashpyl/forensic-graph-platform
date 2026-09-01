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

Swagger UI: <http://localhost:8080/swagger>

### 4. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

The Vite dev server serves the app at **http://localhost:5173**. The API base URL is
read from `frontend/.env.development` (`VITE_API_BASE_URL=http://localhost:8080`). CORS
on the backend allows only that origin, so open the app at `localhost` (not `127.0.0.1`).

### 5. What to click
- Map opens at `/`. Zoom / pan to see clustered severity-coloured markers.
- **+ New Event** → click a spot on the map → fill the dialog.
- Click any marker to open the right-side detail panel (assign persons, link related events, delete).
- **Persons** in the navbar opens the searchable person catalogue.
- **New Person** opens the create-person dialog from anywhere.
- **About** shows a short summary of the project.

### Tests

```bash
cd backend
dotnet test
```

Frontend has a type-checked production build:

```bash
cd frontend
npm run build
```