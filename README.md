# TaskBoard

TaskBoard is a technical interview project: an authenticated task management API and frontend built with ASP.NET Core, PostgreSQL, raw Npgsql, React, TypeScript, Tailwind CSS, and Clean Architecture.

## Local Application Run

Start the local application stack from the repository root:

```powershell
docker compose up -d
```

This starts PostgreSQL, the ASP.NET Core API, and the built React frontend served by nginx.

- Frontend: `http://localhost:5173`
- API: `http://localhost:5141`

If you change backend Docker build inputs and need to force an image rebuild:

```powershell
docker compose up -d --build
```

The API container connects to PostgreSQL on Docker's internal `postgres:5432` address. For optional host-side development, `appsettings.Development.json` points to the same database through host port `15432`.

Run the backend directly from the host when you want the normal .NET inner loop:

```powershell
dotnet build backend\TaskBoard.slnx
dotnet run --project backend\src\TaskBoard.Api\TaskBoard.Api.csproj --launch-profile http
```

In Development, the API initializes the schema with raw SQL scripts and seeds demo data.

Run backend tests:

```powershell
cd backend
dotnet test
```

Seeded credentials:

- Email: `demo@example.com`
- Password: `Demo123!`

## Manual API Testing

Thunder Client files are included under `docs/thunder-client`:

- `TaskBoard_API.thunder-collection.json`
- `TaskBoard_Local.thunder-environment.json`

To use them:

1. In VS Code Thunder Client, import the collection file.
2. Import the environment file and select `TaskBoard Local`.
3. Start the API with the `http` launch profile.
4. Run `GET Health` and `GET Public Ping`.
5. Run `POST Login Demo User`.
6. Copy the `accessToken` response value into the `accessToken` environment variable.
7. Run `GET Current User` or `GET Private Ping`.
8. Run `POST Create Task`, then copy its `id` response value into `taskId` before running task get/update/delete requests.

When new API flows are added, update the Thunder Client collection in the same PR so manual verification stays current.

## Health Checks

The API exposes an anonymous health endpoint:

```http
GET /api/health
GET /health
```

`GET /api/health` returns lightweight status metadata: status, application name, environment, and UTC timestamp. The compatibility `/health` endpoint uses ASP.NET Core health checks and verifies that PostgreSQL is reachable through the configured `DbConnectionFactory`. A healthy application returns HTTP 200 with `Healthy`.

Whenever an essential runtime resource is added, add or update a health check in the same PR. Essential resources include databases, queues, caches, object storage, external APIs, identity providers, or any dependency whose outage means the application cannot serve its core workflow. Health checks should be covered by integration tests when practical and should avoid leaking secrets or sensitive connection details.

## Observability

Observability is intentionally lightweight. I used built-in ASP.NET Core logging, request/correlation IDs, safe exception logging, a health endpoint, and a frontend error boundary. I avoided heavy infrastructure because the goal of the exercise is code quality, Clean Architecture, CRUD, authentication, testing, and explainability.

Backend requests use an `X-Correlation-ID` response header. If the client sends `X-Correlation-ID`, the API preserves it; otherwise, the API generates one and includes it in structured log scopes. Request logging records method, path, status code, elapsed time, correlation ID, authenticated user ID when available, and whether the request succeeded. Unhandled exceptions are logged server-side and returned as safe ProblemDetails responses with `correlationId` and `traceId`, without exposing stack traces in production.

The frontend includes an ErrorBoundary for unexpected React rendering errors. API errors that include `correlationId`, `traceId`, or the `X-Correlation-ID` response header keep that request ID attached to the rejected error so UI error states can display or retain it.

This project does not include Elasticsearch, Prometheus, Grafana, Jaeger, Application Insights, or cloud-specific telemetry services.

## API Endpoints

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/public/ping`
- `GET /api/private/ping`
- `GET /api/tasks`
- `GET /api/tasks/{id}`
- `POST /api/tasks`
- `PUT /api/tasks/{id}`
- `DELETE /api/tasks/{id}`
- `GET /api/health`
- `GET /health`
- `GET /openapi/v1.json` in Development

## Validation

Before backend work is considered done:

```powershell
dotnet build backend\TaskBoard.slnx
cd backend
dotnet test
```

Before runtime wiring or service changes are considered done, verify the Compose stack from the repository root:

```powershell
docker compose up -d
curl.exe -i http://localhost:5141/health
```

Keep `docker-compose.yml` current as new local runtime services are added.

## Frontend Setup

The frontend lives in `frontend` and uses React, TypeScript, and Vite.

Install dependencies:

```powershell
cd frontend
npm install
```

Copy the environment sample if local overrides are needed:

```powershell
Copy-Item .env.example .env.local
```

The default frontend API base URL is `http://localhost:5141`, matching the Compose-exposed API port.

Run the development server:

```powershell
npm run dev
```

The same frontend is also part of the root Docker Compose stack as a production-style static build and is exposed at `http://localhost:5173`.

Build the frontend:

```powershell
npm run build
```

Current routes:

- `/login` for the seeded demo user or registered accounts.
- `/register` to create an account and sign in automatically.
- `/dashboard`, protected by the frontend auth state, for listing, modal create/edit, delete, and drag-and-drop status updates.

The frontend stores demo access and refresh tokens in `localStorage` for this interview slice and sends the access token as a bearer token on API requests.
