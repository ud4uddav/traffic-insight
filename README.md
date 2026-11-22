# Traffic Insight

Traffic Insight is a live Finnish road traffic dashboard. A React/Vite frontend calls a small ASP.NET Core API, which reads current traffic measurements and station metadata from Digitraffic. The dashboard refreshes automatically every minute and can also be refreshed on demand. It does not store or generate traffic measurements.

## Requirements

- .NET 10 SDK
- Node.js 22.12+ and npm

## Run locally

1. Start the API from the repository root:

   ```sh
   dotnet run --project backend/TrafficInsight.Api
   ```

   The HTTP launch profile uses `http://localhost:5278`. The API provides `/health` and `/api/traffic/external`; Swagger is enabled in Development.

2. In another terminal, set `frontend/.env`:

   ```dotenv
   VITE_API_URL=http://localhost:5278/api/traffic
   ```

   The repository includes `frontend/.env.example` with this local value.

3. Install and run the frontend:

   ```sh
   cd frontend
   npm ci
   npm run dev
   ```

   Open the Vite URL, normally `http://localhost:5173`.

## Data behavior

The API reads current TMS station data and station names from Digitraffic. It returns the moving 5 minute average speed as `averageSpeedKmh`, hourly vehicle volume as `vehiclesPerHour`, station names when available, and measurement timestamps. Missing readings remain null and appear as “Not available”; unavailable speed values are excluded from the speed chart. Station metadata is cached in memory for six hours. Live values depend on source availability and sensor coverage.

Digitraffic road data documentation: <https://www.digitraffic.fi/en/road-traffic/>. Attribute Digitraffic/Fintraffic when redistributing the open data and review its current license and API usage instructions before release.

## Deployment preparation

Build the frontend with the production API URL set as `VITE_API_URL`; Vite embeds this value at build time, so a changed API URL requires a new frontend build. Configure the backend with:

- `AllowedOrigins__0`: the exact deployed frontend origin, such as `https://traffic.example.com`.
- `ASPNETCORE_ENVIRONMENT=Production`.

Serve both frontend and API over HTTPS. Keep Swagger restricted to development unless the deployment adds an access policy. No cloud provider or paid service is assumed.

## Checks

From `frontend/`: `npm run lint`, `npm run build`, and `npm audit`.

From the repository root: `dotnet test backend/TrafficInsight.slnx`.
