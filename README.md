# Outpost

A full‑stack family management starter. Monorepo with:

- Backend: ASP.NET Core 8 Web API (JWT auth, SignalR, EF Core with Postgres)
- Frontend: SvelteKit + Vite + TypeScript (PWA, offline queue for writes)
- Database: Postgres schema and policies tailored for Supabase

This ships basic modules for Tasks, Bills, Documents (via Supabase Storage), and Finance link workflows (stubbed for Plaid/Finicity). Auth is currently scaffolded with placeholder logic for rapid local iteration.

## Repo Structure

- `backend/` — ASP.NET Core API (`FamilyManagement.API`)
- `frontend/` — SvelteKit web app
- `database/` — SQL migrations and Supabase notes

## Prerequisites

- .NET SDK 8.x
- Node.js 20+ and npm (or pnpm/yarn)
- Postgres 14+ (or a Supabase project)
- Optional: Docker for running local Postgres

## Quick Start

1) Database

- Using local Postgres (example via Docker):

  ```bash
  docker run --name famlio-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=famlio -p 5432:5432 -d postgres:15
  # After Postgres starts, apply schema:
  psql postgresql://postgres:postgres@localhost:5432/famlio -f database/migrations.sql
  ```

- Using Supabase:
  - Create a project, then run the contents of `database/migrations.sql` in the SQL editor
  - Create a private storage bucket named `documents` (matches code expectations)
  - See `database/supabase-config.md` for notes

2) Environment Variables

Backend (ASP.NET Core):

- `ASPNETCORE_URLS`: recommended `http://localhost:5000` for local dev
- `SUPABASE_DB_CONNECTION`: Postgres connection string
  - Example: `Host=localhost;Database=famlio;Username=postgres;Password=postgres`
- `BACKEND_JWT_SECRET`: HMAC secret for access tokens (use a long random string)
- Optional JWT validation for Supabase tokens:
  - `SUPABASE_JWT_ISSUER`
  - `SUPABASE_JWT_AUDIENCE`
- Optional CORS origins in config (`Cors:AllowedOrigins`) if not using defaults

Frontend (SvelteKit): create `frontend/.env`

```
VITE_API_BASE=http://localhost:5000
VITE_SUPABASE_URL=YOUR_SUPABASE_URL
VITE_SUPABASE_ANON_KEY=YOUR_SUPABASE_ANON_KEY
```

3) Run the Backend

```bash
# from repo root
export ASPNETCORE_URLS=http://localhost:5000
export SUPABASE_DB_CONNECTION="Host=localhost;Database=famlio;Username=postgres;Password=postgres"
export BACKEND_JWT_SECRET="change-me-very-long-random"

dotnet restore backend/FamilyManagement.API/FamilyManagement.API.csproj
DOTNET_ENVIRONMENT=Development dotnet run --project backend/FamilyManagement.API/FamilyManagement.API.csproj
```

- Health check: `GET http://localhost:5000/health` → `{ "status": "ok" }`

4) Run the Frontend

```bash
cd frontend
npm install
npm run dev
# App at http://localhost:5173
```

## Docker Compose (Dev)

Run Postgres, API (dotnet watch), and Frontend (Vite dev) together:

```bash
docker compose up --build
```

- Frontend: http://localhost:5173
- API: http://localhost:5000 (health at /health)
- Postgres: localhost:5432 (db: famlio / postgres:postgres)

Environment overrides:

- API connection string is set to the `db` service. To use a local Postgres instead, change `SUPABASE_DB_CONNECTION` in `docker-compose.yml`.
- Frontend uses `VITE_API_BASE` (defaults to `http://localhost:5000` in code). If needed, set in `docker-compose.yml` or `frontend/.env`.

## API Overview (quick)

Base URL defaults to `http://localhost:5000`.

- Auth (placeholders for local dev):
  - `POST /api/auth/login` — `{ email, password }` → `{ accessToken, refreshToken, expiresAt }`
  - `POST /api/auth/register` — `{ email, password }` → tokens
  - `POST /api/auth/refresh` — `{ refreshToken }` → new tokens
- Tasks (requires `Authorization: Bearer <token>`):
  - `GET /api/tasks?familyId=<uuid>`
  - `GET /api/tasks/{id}`
  - `POST /api/tasks` — TaskItem JSON
  - `PUT /api/tasks/{id}` — TaskItem JSON
  - `DELETE /api/tasks/{id}`
- Bills (requires auth):
  - `GET /api/bills?familyId=<uuid>` and CRUD as above
- Documents (requires auth):
  - `GET /api/documents?familyId=<uuid>`
  - `POST /api/documents/upload` — multipart form `{ familyId, file }` (stored in bucket `documents`)
  - `GET /api/documents/{id}`
  - `GET /api/documents/{id}/signed-url`
  - `DELETE /api/documents/{id}`
- Finance (requires auth unless noted):
  - `POST /api/finance/link-token` — `{ familyId }` → `{ linkToken }`
  - `POST /api/finance/exchange-token` — `{ familyId, provider, publicToken }`
  - `POST /api/finance/webhook/{provider}` — public webhook receiver
- Realtime: SignalR hub at `/hubs/notifications`

## Notes & Caveats

- Auth, Supabase Storage, and Finance integrations are stubbed to ease local development. Replace stubs in `backend/FamilyManagement.API/Services` for production.
- EF Core is configured for Postgres; schema is defined in SQL (`database/migrations.sql`) to align with Supabase features and RLS policies.
- The frontend PWA caches GETs and queues non‑GET requests while offline; queued writes replay when connectivity returns.

## Troubleshooting

- CORS: If the frontend cannot reach the API, ensure the API is on `http://localhost:5000` and either set `Cors:AllowedOrigins` or use the defaults which include `http://localhost:5173`.
- Database: Verify `SUPABASE_DB_CONNECTION` points to a reachable Postgres instance and that `database/migrations.sql` has been applied.
- Tokens: Use a strong `BACKEND_JWT_SECRET`; the default dev fallback is for scaffolding only.

## License

Proprietary — internal project scaffolding unless stated otherwise.
