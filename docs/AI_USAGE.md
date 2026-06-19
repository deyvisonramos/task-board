# AI Usage

## Phase: Domain and Application Layers

### Prompt used with Codex

Implement the Domain and Application layers using TDD for TaskBoard, limited to domain entities/enums, Application DTOs, Result type, repository and service interfaces, TaskService, AuthService, and unit tests. Do not add database code, controllers, JWT implementation, Npgsql, EF, Dapper, or MediatR.

### Representative generated code

- `TaskBoard.Domain.Tasks.TaskItem` and `TaskItemStatus`.
- `TaskBoard.Domain.Users.AppUser`.
- `TaskBoard.Application.Common.Result`.
- `TaskBoard.Application.Tasks.TaskService`.
- `TaskBoard.Application.Auth.AuthService`.
- Unit tests covering task validation, task ownership, registration, duplicate email, and login success/failure.

### How the output was validated

- Unit tests were written against the Application service boundary.
- `dotnet test` is the validation command for this phase.

### What was corrected

- This phase intentionally keeps persistence, controllers, JWT creation, and password hasher implementation out of scope.
- Token creation and password hashing are represented as Application abstractions only.

### Edge cases

- Whitespace task titles fail validation.
- Task titles over 100 characters fail validation.
- Task descriptions over 1000 characters fail validation.
- Missing due dates fail validation.
- Cross-user update and delete attempts fail without mutating the task.

### Authentication decisions

- Registration checks duplicate email before creating a user.
- Registration stores only the password hash returned by `IPasswordHasher`.
- Login returns a generic invalid-credentials failure for missing users or invalid passwords.
- Access and refresh token creation is delegated to `ITokenService`.

### Validation decisions

- Application services return a shared `Result` type.
- Validation failures include code/message pairs.
- Ownership failures currently return `Task.NotFound` to avoid leaking task existence across users.

## Phase: Infrastructure Data Layer

### Prompt used with Codex

Implement the Infrastructure data layer using PostgreSQL and raw Npgsql, limited to `DbConnectionFactory`, `DbInitializer`, `schema.sql`, `seed.sql`, PostgreSQL task/user repositories, and repository integration tests. Do not implement controllers or frontend code, and do not use EF, Dapper, or MediatR.

### Representative generated code

- `TaskBoard.Infrastructure.Persistence.DbConnectionFactory`.
- `TaskBoard.Infrastructure.Persistence.DbInitializer`.
- `TaskBoard.Infrastructure.Persistence.UserRepository`.
- `TaskBoard.Infrastructure.Persistence.TaskRepository`.
- `Database/schema.sql` and `Database/seed.sql`.
- Testcontainers-based repository integration tests.

### How the output was validated

- `dotnet test` passed the existing unit tests and the new Testcontainers-backed PostgreSQL repository and initializer integration tests.
- A static search confirmed SQL statements are confined to the Infrastructure project.

### What was corrected

- Repository tests avoid cleanup SQL in the test project by using unique test records.
- The initializer verifies the target database connection, ensures a migration history table exists, and records applied schema/seed scripts so they are not re-run.

### Edge cases

- Duplicate user email is enforced by the PostgreSQL unique constraint and covered by an integration test.
- Task listing filters by `user_id` and does not return another user's tasks.
- Nullable task descriptions and nullable `updated_at` seed values are mapped safely.

### Authentication decisions

- The demo user is seeded with an ASP.NET Core Identity-compatible password hash for `Demo123!`.
- Password hashes remain repository data and are not exposed through API contracts in this phase.

### Validation decisions

- Repository writes use Npgsql parameters for user and task values.
- Task status is stored as the enum name with a database check constraint.
- `due_date` is stored as `timestamptz` so due dates preserve full UTC timestamps.

## Phase: Authentication Infrastructure

### Prompt used with Codex

Implement authentication infrastructure only: ASP.NET Core password hashing, JWT token creation, options/configuration, dependency injection registration, and focused tests. Do not implement controllers, frontend, repository behavior changes, EF, Dapper, or MediatR.

### Representative generated code

- `TaskBoard.Infrastructure.Auth.PasswordHasher`.
- `TaskBoard.Infrastructure.Auth.JwtTokenService`.
- `TaskBoard.Infrastructure.Auth.JwtOptions`.
- `TaskBoard.Infrastructure.DependencyInjection`.
- Focused unit tests for password hashing, token claims, token expiration, and DTO password-hash leakage.

### How the output was validated

- `dotnet test` passed the existing tests and the focused auth infrastructure tests.

### What was corrected

- Concrete password hashing and token creation now live in Infrastructure behind Application interfaces.
- JWT signing configuration is represented through `JwtOptions` and `appsettings.json`.

### Edge cases

- JWT configuration fails fast when the signing key is missing or too short.
- Refresh tokens are generated separately from access tokens using random bytes.

### Authentication decisions

- Password hashing uses ASP.NET Core `PasswordHasher`.
- JWT access tokens include issuer, audience, user id (`sub`), email, token id, issued-at, not-before, and expiration claims.
- Access token expiration is configured in minutes.
- Refresh token lifetime is configured for future persistence/rotation work, but refresh-token storage is intentionally out of scope for this PR.

### Validation decisions

- Auth API contracts continue to return `UserDto`, which does not expose `PasswordHash`.

## Phase: Auth API

### Prompt used with Codex

Implement the Auth API, including Program.cs dependency injection, JWT bearer authentication setup, AuthController, public/private ping endpoints, and API integration tests. Keep controllers thin, call Application services, map Result objects to HTTP responses, avoid SQL/business validation in controllers, do not expose password hashes, and do not use EF, Dapper, or MediatR.

### Representative generated code

- `TaskBoard.Api.Controllers.AuthController`.
- `TaskBoard.Api.Controllers.PrivateController`.
- `TaskBoard.Api.Controllers.ApiControllerBase`.
- `TaskBoard.Api.Responses.ApiErrorResponse`.
- JWT bearer setup and Infrastructure registration in `Program.cs`.
- `AuthService.GetCurrentUserAsync`.
- WebApplicationFactory/Testcontainers-backed auth API integration tests.

### How the output was validated

- `dotnet build backend\TaskBoard.slnx` passed.
- `dotnet test` from `backend` passed all unit and integration tests.

### What was corrected

- Infrastructure SQL scripts are now copied to build and publish output so startup migrations work from API and test output directories.
- API integration tests replace the concrete `DbConnectionFactory` in test services so the app startup initializer and repositories use the Testcontainers PostgreSQL connection.

### Edge cases

- Invalid login credentials return 401 without revealing whether the email exists.
- Anonymous requests to protected endpoints return 401.
- `/api/auth/me` rejects missing or invalid subject claims through the shared Result-to-response mapper.

### Authentication decisions

- JWT bearer validation uses the configured issuer, audience, signing key, lifetime, and signing key validation.
- Inbound JWT claim mapping is disabled so the API reads the `sub` claim emitted by `JwtTokenService`.
- `/api/auth/me` returns `UserDto` only, never password hashes.

### Validation decisions

- Controllers delegate credential validation to `AuthService`.
- Controller failures use the shared API error response format, including validation code/message arrays when Application services return validation failures.

## Phase: Manual Testing and Health Checks

### Prompt used with Codex

Add a Thunder Client collection for manually calling the API flows and update documentation with those instructions. Add health checks and document that future essential runtime resources should add health checks so the application can report whether it is healthy.

### Representative generated code

- `TaskBoard.Api.HealthChecks.PostgreSqlHealthCheck`.
- `/health` endpoint registration in `Program.cs`.
- Thunder Client collection and local environment files under `docs/thunder-client`.
- README instructions for manual API testing and health-check expectations.

### How the output was validated

- Integration tests include an anonymous `/health` check.
- Backend build and test commands were run after the change.

### What was corrected

- Health checks are anonymous like public operational endpoints while the default API authorization policy still protects application routes.
- The PostgreSQL health check uses the existing `DbConnectionFactory` instead of adding a new health-check package.

### Edge cases

- The health endpoint returns unhealthy if PostgreSQL cannot be reached.
- Protected manual test requests in Thunder Client use an `accessToken` environment variable so the token can be refreshed after login.

### Authentication decisions

- `/health` is explicitly anonymous because it must be callable without a user token.
- Protected Thunder Client requests include an `Authorization: Bearer {{accessToken}}` header.

### Validation decisions

- Essential runtime dependencies should add health checks in the same PR that introduces them.
- Health checks should not expose secrets or sensitive connection details.

## Phase: Protected Task CRUD API

### Prompt used with Codex

Implement the protected Task CRUD API: `TasksController`, current-user claim helper if needed, and API integration tests for authenticated task CRUD. Keep controllers thin, source ownership from JWT claims, keep validation in Application, keep SQL in Infrastructure, and do not use EF, Dapper, or MediatR.

### Representative generated code

- `TaskBoard.Api.Controllers.TasksController`.
- Shared `GetCurrentUserId` and non-generic result mapping in `ApiControllerBase`.
- JSON enum conversion for readable task status values.
- WebApplicationFactory/Testcontainers-backed task API integration tests.
- Thunder Client task CRUD requests.

### How the output was validated

- `dotnet build backend\TaskBoard.slnx` passed.
- `dotnet test` from `backend` passed all unit and integration tests.

### What was corrected

- The private auth-controller claim parser was moved into the shared API controller base for reuse.
- `Task.NotFound` now maps to HTTP 404 so cross-user task access does not leak ownership details.

### Edge cases

- Anonymous task requests return 401.
- Cross-user get, update, and delete attempts return 404.
- Invalid titles return the shared validation error response.
- A submitted `userId` field on create is ignored because task ownership comes from the JWT subject claim.

### Authentication decisions

- Task endpoints are explicitly marked with `[Authorize]`.
- Task ownership is derived only from the `sub` claim in the authenticated access token.
- JWT bearer validation rejects access tokens whose `sub` claim is missing or is not a GUID, so controllers treat the current user id as an authenticated invariant.

### Validation decisions

- Task request DTOs do not include `UserId`.
- Task validation remains in `TaskService` and returns code/message validation items through the shared API error format.

## Phase: Validation and Auth Pipeline Cleanup

### Prompt used with Codex

Use the open-source FluentValidation line before the license change to validate request model inputs, discover validators from the assembly where the request models live, and return validation errors in the standard API response format. Move authentication behavior back into ASP.NET Core middleware/pipeline patterns instead of mapping authentication failures manually in controller response helpers, and update the project harness so the pattern does not regress.

### Representative generated code

- `RegisterRequestValidator` and `LoginRequestValidator` in the Application assembly.
- `FluentValidationActionFilter` for automatic controller argument validation.
- MVC invalid model-state response customization in `Program.cs`.
- JWT bearer `OnTokenValidated` subject validation in `Program.cs`.
- API convention tests that prevent manual auth failure mapping in `ApiControllerBase`.

### How the output was validated

- `dotnet build backend\TaskBoard.slnx` passed.
- `dotnet test --no-build` passed all unit and integration tests.

### What was corrected

- `ApiControllerBase` no longer maps `Auth.*` failures to `Unauthorized`; authentication challenges are handled by ASP.NET Core authentication/authorization middleware.
- Invalid login credentials now return the standard application failure response as `400 Bad Request`; protected endpoints still return `401 Unauthorized` through middleware.
- Invalid request DTOs are validated before controller actions and returned as `ApiErrorResponse` with validation code/message items.

### Edge cases

- Invalid email formats return `Auth.EmailInvalid`.
- JWTs without a valid GUID `sub` claim fail token validation before protected actions run.

### Authentication decisions

- Default authorization remains enforced through the fallback authorization policy.
- Public endpoints continue to use `AllowAnonymous`.

### Validation decisions

- FluentValidation is pinned to the 11.x package line.
- Validators live beside the request DTOs in `TaskBoard.Application`.
- The API registers validators by assembly and uses one validation filter for controller arguments.

## Phase: Local Development Setup

### Prompt used with Codex

Add local development setup for this PR: root `docker-compose.yml` with PostgreSQL and API services, backend Dockerfile, Development connection string, Development-only startup database initialization, demo seed credentials and tasks, README backend run instructions, and validation with `docker compose up -d`, `dotnet test`, and API health/login probes.

### Representative generated code

- Root `docker-compose.yml` for local PostgreSQL and the ASP.NET Core API.
- Backend `Dockerfile` and `.dockerignore`.
- `appsettings.Development.json` local PostgreSQL connection string.
- Development-only `DbInitializer` startup execution in `Program.cs`.
- Raw SQL schema and seed updates for full UTC task due timestamps.
- README local backend run instructions.

### How the output was validated

- `docker compose up -d` started PostgreSQL and the API.
- `dotnet build backend\TaskBoard.slnx` passed.
- `dotnet test` from `backend` passed all unit and integration tests.
- `GET /health` returned `200 OK` from the Compose-hosted API.
- `POST /api/auth/login` succeeded with `demo@example.com` / `Demo123!`.

### What was corrected

- The Compose service avoids a fixed container name so local reviewer machines do not collide with existing containers.
- The host database port is `15432` to avoid common local PostgreSQL conflicts on `5432`.
- Startup schema/seed execution is limited to Development.
- Compose now runs the backend API too, so reviewers do not need a separate host `dotnet run` for the normal local stack.

### Edge cases

- Re-running the initializer remains application-level idempotent through the migration history table.
- Demo seed inserts use deterministic IDs and conflict handling.

### Authentication decisions

- Demo credentials are local-only and use the existing ASP.NET Core Identity-compatible password hash.
- Login responses continue to return user DTOs and tokens, never password hashes.

### Validation decisions

- Schema and seed remain raw SQL in the Infrastructure layer.
- No EF, Dapper, or MediatR packages were added.
- The project harness now requires `docker-compose.yml` to stay current and runnable as local runtime services are added or changed.

## Phase: Frontend Skeleton

### Prompt used with Codex

Set up the frontend skeleton: React and TypeScript app structure, basic routing, empty Login/Register/Dashboard pages, API client base setup, CSS reset/layout, `.env.example`, and README frontend setup instructions. Do not implement real auth, task CRUD, or add a heavy UI framework.

### Representative generated code

- `src/App.tsx` with routes for `/login`, `/register`, and `/dashboard`.
- `src/main.tsx` with `BrowserRouter` setup.
- Empty page shells under `src/pages`.
- Shared Axios client in `src/api/httpClient.ts`.
- Endpoint-facing API modules in `src/api/authApi.ts` and `src/api/tasksApi.ts`.
- Basic reset and layout styles in `src/index.css`.

### How the output was validated

- `npm run build` passed from `frontend`.
- `npm run lint` passed from `frontend`.
- The Vite dev server started successfully.
- A browser check rendered `/login`, `/register`, and `/dashboard` with the expected page headings.
- `docker compose up -d` started PostgreSQL, the API, and the frontend.
- `GET /health` returned `200 OK` from the Compose-hosted API.
- `GET /login` returned the Compose-hosted frontend HTML.
- A browser check against the Compose-hosted frontend rendered `/login`, `/register`, and `/dashboard` without console warnings or errors.

### What was corrected

- Removed the default Vite starter UI and unused starter assets.
- Updated the frontend API base URL sample to match the Compose-exposed API port, `http://localhost:5141`.
- Replaced template README content with TaskBoard frontend setup instructions.
- Added the frontend service to the root Docker Compose stack after identifying that the skeleton is now part of the local runtime.

### Edge cases

- Unknown routes redirect to `/login`.
- The root route redirects to `/login`.
- Navigation highlights the active route.

### Authentication decisions

- No real authentication flow is implemented in this phase.
- Auth API function shapes are prepared for a later auth UI slice.

### Validation decisions

- No frontend form validation is implemented because login and registration forms are out of scope.
- API base URL configuration uses `VITE_API_BASE_URL` with a local default.
