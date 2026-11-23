# Traffic Insight

Live Finnish road traffic dashboard built with React and ASP.NET Core. It reads current traffic measurements from Digitraffic, refreshes them every minute, and presents station readings and analytics in one place.

## Features

- Live station readings for vehicle volume and average speed
- Search by station name or station ID
- Summary cards and charts based on the latest reported values
- Clear handling for missing measurements; unavailable values are never shown as zero
- Station names from Digitraffic metadata, with station ID fallback
- Read-only design with no manual or generated measurements

## Architecture

```mermaid
flowchart LR
    Visitor[Visitor] --> Browser[React + Vite dashboard]
    Browser -->|JSON| Api[ASP.NET Core API]
    Api --> Service[Digitraffic service]
    Service -->|Current readings| TMS[Digitraffic TMS]
    Service -->|Station names| Metadata[Digitraffic station metadata]
    Service <--> Cache[(In-memory metadata cache<br/>6 hours)]
```

The browser calls the app API. The API fetches live TMS readings and station metadata from Digitraffic, maps them into a compact response, and caches station metadata in memory for six hours. Current readings are fetched for each API request; the frontend refreshes every minute.

### Data storage

This application has **no database**. It does not save traffic history or user data. The only cache is temporary station metadata in the running API process. A persistent database such as PostgreSQL would make sense if the app later needs historical snapshots, accounts, saved preferences, or reports that must remain available when Digitraffic is unavailable.

## Technology

| Area | Technologies |
| --- | --- |
| Frontend | React, Vite, Recharts |
| Backend | ASP.NET Core Minimal API, .NET 10 |
| Live data | Digitraffic / Fintraffic TMS |
| Tests | xUnit backend tests; Node.js frontend unit tests, ESLint, and production build checks |

## Run locally without Docker

### Requirements

- .NET 10 SDK
- Node.js 22.12 or later and npm

### 1. Start the API

From the repository root:

```sh
dotnet run --project backend/TrafficInsight.Api
```

The HTTP launch profile listens at `http://localhost:5278`. Available endpoints:

- `GET /health` — health status
- `GET /api/traffic/external` — current station measurements

Swagger is available only when the API runs in Development.

### 2. Configure and start the frontend

In a second terminal, create `frontend/.env` using the committed example:

```sh
cp frontend/.env.example frontend/.env
```

Then install dependencies and start Vite:

```sh
cd frontend
npm ci
npm run dev
```

Open the local Vite address shown in the terminal, normally `http://localhost:5173`.

## Run with Docker Compose

Docker Desktop or another Docker Engine with Compose v2 is required.

```sh
docker compose up --build
```

Open [http://localhost:8080](http://localhost:8080). The frontend container serves the React build and proxies `/api` requests to the private API container. The API is not published as a host port. Stop the services with `Ctrl+C`, then run `docker compose down`.

Optional local settings can be placed in a root `.env` file, which is ignored by Git:

```dotenv
FRONTEND_PORT=8080
ALLOWED_ORIGIN=http://localhost:8080
DIGITRAFFIC_BASE_URL=https://tie.digitraffic.fi/
DIGITRAFFIC_TIMEOUT_SECONDS=15
TRAFFIC_RATE_LIMIT_PER_MINUTE=120
```

The API also supports standard .NET environment variable configuration directly: `Digitraffic__BaseUrl`, `Digitraffic__RequestTimeoutSeconds`, `Traffic__RateLimitPerMinute`, and `AllowedOrigins__0`. Values in `frontend/.env` are public browser settings; do not place secrets in them.

## Data notes

The API maps Digitraffic's five-minute moving average speed to `averageSpeedKmh` and vehicle count data to `vehiclesPerHour`. Missing source values remain `null`; the UI shows “Not available” and excludes unavailable speed values from the speed chart. Station names may be unavailable, in which case the dashboard uses the station ID. Live availability and sensor coverage depend on Digitraffic.

The dashboard's summary speed is the arithmetic mean of stations with a reported speed. It is not a traffic-volume-weighted national average.

See [Digitraffic road traffic documentation](https://www.digitraffic.fi/en/road-traffic/). Follow its current API usage instructions and attribution/license terms when redistributing the data.

## Tests and quality checks

Run backend tests from the repository root:

```sh
dotnet test backend/TrafficInsight.slnx
```

Run frontend checks from `frontend/`:

```sh
npm run lint
npm test
npm run build
npm audit
```

Check .NET dependencies for known vulnerabilities:

```sh
dotnet list backend/TrafficInsight.slnx package --vulnerable --include-transitive
```

The backend unit tests cover service mapping, missing speed values, and metadata fallback. Frontend unit tests cover average-speed calculation and missing-speed handling. The project does not yet have API integration tests or browser end-to-end tests.

## Security and deployment notes

- The dashboard is public and read-only; it has no user accounts or private data.
- CORS uses configured origins. The local default allows Vite at `http://localhost:5173`; a hosted deployment must set `AllowedOrigins__0` to the exact frontend origin.
- Swagger is disabled outside Development.
- The API requires an HTTPS upstream URL, applies a configurable 15-second Digitraffic timeout, rate limits traffic requests, and returns sanitized upstream errors.
- Production API logs are structured JSON. Health endpoints are available at `/health` (API) and `/healthz` (frontend).
- CORS is not API access control. The API is read-only and rate-limited; HTTPS should be provided by the hosting platform.
- `VITE_*` values are included in browser assets. Never place passwords, private keys, or other secrets in them.

## Continuous integration and hosting

GitHub Actions runs on pushes and pull requests. It tests, lints, audits, and builds the frontend, tests and checks the backend dependencies, and builds both Docker images. It does not deploy to a cloud platform.

The application has no database and does not need PostgreSQL. Render deployment can use a static site for React and a web service for the .NET API. Set `VITE_API_URL` on the frontend to the API's `/api/traffic` URL, and set `AllowedOrigins__0` on the API to the exact frontend URL. The API's `ASPNETCORE_HTTP_PORTS` must match the web service's configured port. The free Render web-service plan sleeps after inactivity, so the first API request may take about a minute to wake it.

## Repository layout

```text
backend/
  TrafficInsight.Api/         ASP.NET Core API and Digitraffic integration
  TrafficInsight.Api.Tests/    Backend unit tests
  TrafficInsight.slnx          .NET solution
frontend/
  src/                         React dashboard and chart components
  public/                      Static public assets
  .env.example                  Local API URL example
compose.yaml                    Local multi-container setup
.github/workflows/ci.yml        Automated tests and Docker builds
```
