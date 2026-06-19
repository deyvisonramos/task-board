# TaskBoard

TaskBoard is a technical interview project for an authenticated task management application. It includes an ASP.NET Core Web API, PostgreSQL persistence through raw SQL and Npgsql, and a React + TypeScript frontend.

## User Story

As a registered user, I want to create, view, update, and delete my tasks with title, description, status, and due date, so that I can manage my work from a simple dashboard.

## Architecture

The backend follows Clean Architecture with explicit project boundaries:

- `TaskBoard.Domain`: domain entities and enums.
- `TaskBoard.Application`: use cases, interfaces, DTOs, validation, and business rules.
- `TaskBoard.Infrastructure`: raw SQL persistence, Npgsql connection handling, password hashing, JWT token creation, database initialization, and seed data.
- `TaskBoard.Api`: controllers, authentication/authorization middleware, error responses, health checks, CORS, logging, and API startup wiring.
- `TaskBoard.UnitTests`: fast tests for application and infrastructure behavior.
- `TaskBoard.IntegrationTests`: API and repository tests backed by PostgreSQL through Testcontainers.

Dependency direction is inward: Domain depends on nothing, Application depends on Domain, Infrastructure depends on Application and Domain, and API composes the application by referencing Application, Infrastructure, and Domain.

Business rules stay out of controllers. Data access stays in Infrastructure. SQL is parameterized and kept in Infrastructure. Authentication uses ASP.NET Core JWT authentication and authorization middleware.

## Tech Stack

- Backend: ASP.NET Core Web API, C#, FluentValidation 11.x
- Database: PostgreSQL
- Data access: raw SQL with Npgsql
- Auth: JWT access tokens, refresh tokens, ASP.NET Core `PasswordHasher`
- Frontend: React, TypeScript, Vite, plain CSS
- Runtime: Docker Compose
- Tests: xUnit, Testcontainers

Entity Framework, Dapper, MediatR, and mediator-style libraries are not used.

## Quick Start With Docker Compose

From the repository root:

```powershell
docker compose up -d --build
```

This starts:

- Frontend: `http://localhost:5173`
- API: `http://localhost:5141`
- PostgreSQL: host port `15432`, container port `5432`

Smoke-test the running stack:

```powershell
curl.exe -i http://localhost:5141/health
curl.exe -i http://localhost:5141/api/public/ping
```

Stop the stack:

```powershell
docker compose down
```

Remove the PostgreSQL volume if you want to reset seeded data:

```powershell
docker compose down -v
```

## Seeded Credentials

The Development startup path initializes the database schema and demo data.

- Email: `demo@example.com`
- Password: `Demo123!`

The demo user has seeded tasks in `Todo`, `InProgress`, and `Done`.

## Backend Commands

The backend solution is in `backend`.

Build from the repository root:

```powershell
dotnet build backend\TaskBoard.slnx
```

Run tests:

```powershell
cd backend
dotnet test
```

Run the API from the host:

```powershell
dotnet run --project backend\src\TaskBoard.Api\TaskBoard.Api.csproj --launch-profile http
```

When running the API directly, keep PostgreSQL available. The easiest option is to start the Compose database:

```powershell
docker compose up -d postgres
```

The host-side Development connection string uses `localhost:15432`.

## Frontend Commands

The frontend app is in `frontend`.

Install dependencies:

```powershell
cd frontend
npm install
```

Run the development server:

```powershell
npm run dev
```

Build the frontend:

```powershell
npm run build
```

The default API base URL is `http://localhost:5141`. To override it locally, copy `frontend\.env.example` to `frontend\.env.local` and change `VITE_API_BASE_URL`.

## Test Commands

Backend validation:

```powershell
dotnet build backend\TaskBoard.slnx
cd backend
dotnet test
```

Frontend validation:

```powershell
cd frontend
npm run build
```

Runtime validation after Compose or service-wiring changes:

```powershell
docker compose up -d --build
curl.exe -i http://localhost:5141/health
curl.exe -i http://localhost:5141/api/public/ping
```

## API Endpoints

Auth:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`

Ping and health:

- `GET /api/public/ping`
- `GET /api/private/ping`
- `GET /api/health`
- `GET /health`

Tasks:

- `GET /api/tasks`
- `GET /api/tasks/{id}`
- `POST /api/tasks`
- `PUT /api/tasks/{id}`
- `DELETE /api/tasks/{id}`

Development OpenAPI document:

- `GET /openapi/v1.json`

Protected endpoints require a bearer access token.

## Demo Script

1. Start the application:

   ```powershell
   docker compose up -d --build
   ```

2. Open `http://localhost:5173`.

3. Log in with the seeded credentials:

   - Email: `demo@example.com`
   - Password: `Demo123!`

4. Review the seeded tasks on the dashboard.

5. Create a task with a title, optional description, status, and due date.

6. Drag a task to another status column.

7. Edit a task and save the changes.

8. Delete a task.

9. Register a new account and confirm the dashboard starts with that user's own task list.

10. Optionally verify API auth directly:

    ```powershell
    curl.exe -i http://localhost:5141/api/private/ping
    ```

    The endpoint should reject anonymous requests. Log in through `POST /api/auth/login`, then retry with `Authorization: Bearer <accessToken>`.

## Error and Validation Format

The API uses a single error response shape for application and validation failures. Validation failures include a `validation` array with item codes and messages. Authentication challenges are handled by ASP.NET Core authentication/authorization middleware rather than controller response helpers.

## Known Tradeoffs

- The current database initialization uses raw SQL schema and seed scripts in Infrastructure and applies them during Development startup. It is intentionally simple for the interview slice.
- Tailwind CSS is part of the target exercise stack, but this checkout currently uses plain CSS instead of Tailwind packages.
- Refresh tokens are returned by auth responses, but the frontend currently stores tokens in `localStorage` for a simple demo workflow.
- The frontend is intentionally lightweight: it covers login, registration, dashboard task CRUD, drag-and-drop status changes, loading states, error states, empty states, and responsive layout without adding a larger UI framework.
- Observability is intentionally minimal: built-in ASP.NET Core logging, correlation IDs, health checks, safe exception handling, and a React error boundary.
- Docker Compose is the supported full-stack local runtime. Host-side backend and frontend commands are available for the development inner loop.
