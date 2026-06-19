# TaskBoard

TaskBoard is a technical interview project: an authenticated task management API and frontend built with ASP.NET Core, PostgreSQL, raw Npgsql, React, TypeScript, Tailwind CSS, and Clean Architecture.

## Local Backend Run

Start the local backend stack from the repository root:

```powershell
docker compose up -d
```

This starts PostgreSQL and the ASP.NET Core API. The API is available at `http://localhost:5141`.

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
GET /health
```

The current health check verifies that PostgreSQL is reachable through the configured `DbConnectionFactory`. A healthy application returns HTTP 200 with `Healthy`.

Whenever an essential runtime resource is added, add or update a health check in the same PR. Essential resources include databases, queues, caches, object storage, external APIs, identity providers, or any dependency whose outage means the application cannot serve its core workflow. Health checks should be covered by integration tests when practical and should avoid leaking secrets or sensitive connection details.

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

Keep `docker-compose.yml` current as new local runtime services are added, including the future frontend.
