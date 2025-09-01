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
  docker run --name outpost-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=outpost -p 5432:5432 -d postgres:15
  # After Postgres starts, apply schema:
  psql postgresql://postgres:postgres@localhost:5432/outpost -f database/migrations.sql
  ```

- Using Supabase:
  - Create a project, then run the contents of `database/migrations.sql` in the SQL editor
  - Create a private storage bucket named `documents` (matches code expectations)
  - See `database/supabase-config.md` for notes

2) Environment Variables

Backend (ASP.NET Core):

- `ASPNETCORE_URLS`: recommended `http://localhost:5000` for local dev
- `SUPABASE_DB_CONNECTION`: Postgres connection string
  - Example: `Host=localhost;Database=outpost;Username=postgres;Password=postgres`
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
export SUPABASE_DB_CONNECTION="Host=localhost;Database=outpost;Username=postgres;Password=postgres"
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
- Postgres: localhost:5432 (db: outpost / postgres:postgres)

Environment overrides:

- API connection string is set to the `db` service. To use a local Postgres instead, change `SUPABASE_DB_CONNECTION` in `docker-compose.yml`.
- Frontend uses `VITE_API_BASE` (defaults to `http://localhost:5000` in code). If needed, set in `docker-compose.yml` or `frontend/.env`.

## Production Docker Images

Build and run production images:

Backend (ASP.NET Core):

```bash
docker build -t outpost-api:prod -f backend/Dockerfile .
docker run -p 8080:8080 \
  -e ASPNETCORE_URLS=http://0.0.0.0:8080 \
  -e SUPABASE_DB_CONNECTION="Host=localhost;Database=outpost;Username=postgres;Password=postgres" \
  -e BACKEND_JWT_SECRET="change-me-very-long-random" \
  outpost-api:prod
```

Frontend (Nginx static):

```bash
docker build -t outpost-web:prod -f frontend/Dockerfile .
docker run -p 8081:80 outpost-web:prod
```

Notes:

- The frontend uses SvelteKit static adapter and is served via Nginx with SPA fallback.
- Ensure API CORS allows your deployed frontend origin.
- Supply a managed Postgres connection string to the API in production.

## docker-compose.prod.yml

Build and run the production stack (Nginx static frontend + API + Postgres):

```bash
docker compose -f docker-compose.prod.yml up --build -d
```

- Frontend (Nginx): http://localhost:8081
- API: http://localhost:8080 (health at `/health`)
- DB: localhost:5432 (db `outpost`, user `postgres`, password `postgres`)

Notes:

- The frontend is built with `VITE_API_BASE` set to empty, so it uses same-origin requests and Nginx proxies `/api` and `/hubs` to the `api` service inside the Compose network (see `frontend/nginx.conf`). This avoids CORS.
- If you choose not to proxy through Nginx, set `VITE_API_BASE` to the API URL at build time and ensure `Cors__AllowedOrigins` on the API matches your frontend origin.

### Local Hot Reload Override

Use the override to run the frontend in hot-reload mode against the prod API:

```bash
docker compose -f docker-compose.prod.yml -f docker-compose.override.yml up --build
```

- Frontend (dev server): http://localhost:5173
- API (prod container): http://localhost:8080

The override creates a `web-dev` service that bind-mounts `frontend/` and sets `VITE_API_BASE=http://localhost:8080` so the browser talks to the API directly. The static Nginx `web` service is disabled by default via profiles in the override.

## Single-Domain Nginx (Outside Docker)

Use this when deploying on a single host (e.g., a VM) where the API runs locally and Nginx serves the static frontend and reverse-proxies API requests.

1) Build the frontend

```bash
cd frontend
npm ci
npm run build
sudo mkdir -p /var/www/outpost
sudo rsync -a --delete build/ /var/www/outpost/
```

2) Run the API on localhost:8080

- Option A: Use the production Docker image

```bash
docker build -t outpost-api:prod -f backend/Dockerfile .
docker run -d --name outpost-api \
  -p 8080:8080 \
  -e ASPNETCORE_URLS=http://0.0.0.0:8080 \
  -e SUPABASE_DB_CONNECTION="Host=localhost;Database=outpost;Username=postgres;Password=postgres" \
  -e BACKEND_JWT_SECRET="change-me-very-long-random" \
  outpost-api:prod
```

- Option B: Run the published binary directly (ensure .NET runtime installed)

```bash
dotnet publish backend/FamilyManagement.API/FamilyManagement.API.csproj -c Release -o out
ASPNETCORE_URLS=http://0.0.0.0:8080 \
SUPABASE_DB_CONNECTION="Host=localhost;Database=outpost;Username=postgres;Password=postgres" \
BACKEND_JWT_SECRET="change-me-very-long-random" \
dotnet out/FamilyManagement.API.dll
```

3) Install Nginx site

```bash
sudo cp deploy/nginx.single-domain.conf /etc/nginx/sites-available/outpost.conf
sudo sed -i "s/example.com/your-domain.com/g" /etc/nginx/sites-available/outpost.conf
sudo nginx -t
sudo ln -sf /etc/nginx/sites-available/outpost.conf /etc/nginx/sites-enabled/outpost.conf
sudo systemctl reload nginx
```

4) Enable HTTPS (optional but recommended)

```bash
sudo apt-get update && sudo apt-get install -y certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

Notes:

- The Nginx config proxies `/api/*` and `/hubs/*` to the API at `http://127.0.0.1:8080` and serves the static site from `/var/www/outpost`.
- `client_max_body_size 20m` matches the backend documents upload limit.
- Since the app is served from a single domain, CORS is not required.

## systemd Service (API)

Install the API as a systemd service so it starts on boot and restarts on failure.

1) Publish and deploy the API

```bash
dotnet publish backend/FamilyManagement.API/FamilyManagement.API.csproj -c Release -o out
sudo useradd --system --no-create-home --shell /usr/sbin/nologin outpost || true
sudo mkdir -p /opt/outpost/api
sudo rsync -a --delete out/ /opt/outpost/api/
sudo chown -R outpost:outpost /opt/outpost
```

2) Configure environment

```bash
sudo cp deploy/outpost-api.env.example /etc/outpost-api.env
sudo chmod 600 /etc/outpost-api.env
sudo editor /etc/outpost-api.env
```

3) Install and enable the service

```bash
sudo cp deploy/outpost-api.service /etc/systemd/system/outpost-api.service
sudo systemctl daemon-reload
sudo systemctl enable --now outpost-api
```

4) Verify logs and status

```bash
systemctl status outpost-api
journalctl -u outpost-api -f
```

Notes:

- The service runs as the `outpost` user in `/opt/outpost/api` and reads environment variables from `/etc/outpost-api.env`.

## systemd Service (API via Docker)

Run the API as a Docker container managed by systemd. This is useful when deploying a single container on a VM without Compose.

1) Build or publish the image

```bash
# Build locally
docker build -t outpost-api:prod -f backend/Dockerfile .
# Or pull from your registry (adjust IMAGE in the env file)
```

2) Prepare environment files

```bash
sudo cp deploy/outpost-api-docker.env.example /etc/outpost-api-docker.env
sudo cp deploy/outpost-api.env.example /etc/outpost-api.env
sudo chmod 600 /etc/outpost-api-docker.env /etc/outpost-api.env
sudo editor /etc/outpost-api-docker.env   # set IMAGE and API_PORT if needed
sudo editor /etc/outpost-api.env          # set DB connection, JWT secret, etc.
```

3) Install and enable the service

```bash
sudo cp deploy/outpost-api-docker.service /etc/systemd/system/outpost-api-docker.service
sudo systemctl daemon-reload
sudo systemctl enable --now outpost-api-docker
```

4) Verify

```bash
systemctl status outpost-api-docker
journalctl -u outpost-api-docker -f
```

Notes:

- The unit pulls `${IMAGE}` before starting (ignored if unavailable).
- It maps `${API_PORT}` on the host to container port 8080.
- Container environment is read from `/etc/outpost-api.env` via `--env-file`.

## systemd Services (Frontend via Docker) and Target

Run the Nginx-served frontend as a Docker container and optionally orchestrate both services with a target.

1) Build or pull the frontend image

```bash
docker build -t outpost-web:prod -f frontend/Dockerfile .
```

2) Prepare environment file

```bash
sudo cp deploy/outpost-web-docker.env.example /etc/outpost-web-docker.env
sudo chmod 600 /etc/outpost-web-docker.env
sudo editor /etc/outpost-web-docker.env   # set IMAGE and WEB_PORT if needed
```

3) Install and enable the web service

```bash
sudo cp deploy/outpost-web-docker.service /etc/systemd/system/outpost-web-docker.service
sudo systemctl daemon-reload
sudo systemctl enable --now outpost-web-docker
```

4) Optional: Orchestrate both with a target

```bash
sudo cp deploy/outpost.target /etc/systemd/system/outpost.target
sudo systemctl daemon-reload
sudo systemctl enable --now outpost.target
# Now you can start/stop both:
sudo systemctl stop outpost.target
```

Notes:

- Units remain independent for troubleshooting: start/stop `outpost-api-docker` or `outpost-web-docker` as needed.
- The `outpost.target` simply groups them, so `systemctl start outpost.target` starts both.

## Health Checks and Auto-Restart

Two layers of resilience are included:

- Dockerfile HEALTHCHECKs for both API and Web containers
- A systemd timer that curls the endpoints and restarts services if unhealthy

Setup the timer:

```bash
sudo install -m 0755 deploy/outpost-healthcheck.sh /usr/local/bin/outpost-healthcheck.sh
sudo cp deploy/outpost-healthcheck.service /etc/systemd/system/outpost-healthcheck.service
sudo cp deploy/outpost-healthcheck.timer /etc/systemd/system/outpost-healthcheck.timer
sudo systemctl daemon-reload
sudo systemctl enable --now outpost-healthcheck.timer
```

Behavior:

- Checks API at `http://127.0.0.1:${API_PORT}/health` (defaults 8080) and Web at `http://127.0.0.1:${WEB_PORT}/` (defaults 8081).
- Reads `${API_PORT}` from `/etc/outpost-api-docker.env` and `${WEB_PORT}` from `/etc/outpost-web-docker.env` if present.
- Restarts `outpost-api-docker` or `outpost-web-docker` when checks fail.

## Centralized Logs and Alerts

You have a few options to forward logs and trigger alerts.

### Option A: Grafana Loki via Promtail

1) Copy config and env

```bash
sudo mkdir -p /etc/promtail /var/lib/promtail
sudo cp deploy/promtail-config.yml /etc/promtail/promtail.yaml
sudo cp deploy/outpost-promtail-docker.service /etc/systemd/system/outpost-promtail-docker.service
sudo cp deploy/outpost-promtail.env.example /etc/outpost-promtail.env
sudo systemctl daemon-reload
sudo systemctl enable --now outpost-promtail-docker
```

2) Set the Loki URL inside `/etc/promtail/promtail.yaml` clients[].url

Promtail scrapes:
- systemd journal (labels unit, host)
- Docker container logs (`/var/lib/docker/containers/*/*-json.log`)

### Option B: AWS CloudWatch via Fluent Bit

1) Copy config and env

```bash
sudo mkdir -p /etc/fluent-bit
sudo cp deploy/fluent-bit.conf /etc/fluent-bit/fluent-bit.conf
sudo cp deploy/outpost-fluent-bit-docker.service /etc/systemd/system/outpost-fluent-bit-docker.service
sudo cp deploy/outpost-fluent-bit.env.example /etc/outpost-fluent-bit.env
sudo systemctl daemon-reload
sudo systemctl enable --now outpost-fluent-bit-docker
```

2) IAM and region

- Prefer an instance role with CloudWatch Logs permissions: `logs:CreateLogGroup`, `logs:CreateLogStream`, `logs:PutLogEvents`.
- Set `AWS_REGION` in `/etc/outpost-fluent-bit.env`.

Fluent Bit forwards:
- systemd journal units: `outpost-api-docker.service`, `outpost-web-docker.service` → log group `/outpost/systemd`
- Docker container logs → log group `/outpost/docker`

### Simple Webhook Alerts on Failures

Use systemd `OnFailure` to send a Slack (or generic) webhook when a unit fails.

1) Install the alert script and template unit

```bash
sudo install -m 0755 deploy/outpost-alert.sh /usr/local/bin/outpost-alert.sh
sudo cp deploy/outpost-alert@.service /etc/systemd/system/outpost-alert@.service
echo 'SLACK_WEBHOOK_URL=https://hooks.slack.com/services/XXX/YYY/ZZZ' | sudo tee /etc/outpost-alert.env >/dev/null
sudo systemctl daemon-reload
```

2) Add an OnFailure drop-in to the API and Web units

```bash
sudo systemctl edit outpost-api-docker --full --force
# Add: OnFailure=outpost-alert@%n.service
sudo systemctl edit outpost-web-docker --full --force
# Add: OnFailure=outpost-alert@%n.service
sudo systemctl daemon-reload
```

Now if either unit enters a failed state, systemd triggers the alert unit which posts to your webhook.
- Update the unit file if your deployment paths differ.

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
