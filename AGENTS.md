# AGENTS.md

## Project goal

Build a technical interview project named TaskBoard.

TaskBoard is an authenticated task management application using:

- ASP.NET Core Web API
- C#
- PostgreSQL
- Raw SQL with Npgsql
- React
- TypeScript
- Tailwind CSS
- Clean Architecture
- TDD where practical

## Hard constraints

- Do not use Entity Framework.
- Do not use Dapper.
- Do not use MediatR or mediator-style libraries.
- Use raw SQL through Npgsql.
- Use parameterized SQL only.
- Keep SQL inside the Infrastructure layer.
- Keep business rules outside controllers.
- Keep data access outside the Application and Domain layers.
- Every backend PR must pass `dotnet test`.
- Every frontend PR must pass `npm run build`.
- Browser console warnings should be avoided and treated as desired cleanup, matching the exercise guidance.

## Architecture rules

Backend projects:

- TaskBoard.Domain
- TaskBoard.Application
- TaskBoard.Infrastructure
- TaskBoard.Api
- TaskBoard.UnitTests
- TaskBoard.IntegrationTests

Dependency direction:

- Domain depends on nothing.
- Application depends only on Domain.
- Infrastructure depends on Application and Domain.
- API depends on Application, Infrastructure, and Domain.
- Tests may reference the layers they test.

Runtime and deployment:

- Use Docker Compose for the full application, including PostgreSQL, API, and frontend.
- The application should be runnable locally through Docker Compose.
- Keep `docker-compose.yml` up to date as services are added or changed; future frontend, worker, cache, or other runtime services must be included when they become part of the local application.
- When a PR changes runtime wiring, service dependencies, ports, environment variables, database initialization, or adds a new service, verify the local Compose stack with `docker compose up -d` and a relevant smoke test.
- Backend tests should use Testcontainers for integration-test PostgreSQL dependencies.

Code organization and naming:

- Concrete repository classes should be named by their domain role, for example `TaskRepository` and `UserRepository`; do not prefix them with the storage provider such as `Postgres`.
- Keep each top-level class in its own file. Test fixtures and reusable test support classes should be separate top-level files, not nested classes inside test classes.

## Business domain

The app manages tasks.

Task fields:

- Id
- UserId
- Title
- Description
- Status
- DueDate
- CreatedAt
- UpdatedAt

Allowed statuses:

- Todo
- InProgress
- Done

User fields:

- Id
- Email
- PasswordHash
- CreatedAt

Data conventions:

- Use GUIDs for all entity identifiers.
- Store timestamps as full UTC timestamps.
- Treat DueDate as a full UTC timestamp.

Business rules:

- Title is required.
- Title must be 100 characters or fewer.
- Description must be 1000 characters or fewer.
- DueDate is required.
- A user can only access their own tasks.
- Passwords must be hashed.
- Use ASP.NET Core PasswordHasher for password hashing.
- Password hashes must never be returned by the API.

Authentication rules:

- Use JWT authentication.
- Support refresh tokens.
- Protected endpoints must reject anonymous requests.
- The API must never return password hashes.
- Use ASP.NET Core authentication/authorization middleware, policies, and handlers for authentication and authorization decisions.
- Do not map authentication failures manually in controller response helpers.
- Keep application result mapping for application failures, not middleware-owned authentication challenges.

API contract rules:

- Use DTOs for incoming requests.
- Use DTOs for API responses where domain entities would expose internal details.
- Use HTTP status codes to represent error categories.
- Use a single error response format across the API.
- Validation responses should follow the same general error format and include a validation array.
- Each validation item should include a message and a code.
- Use FluentValidation 11.x for request DTO validation.
- Keep validators in the same assembly as the request DTOs they validate.
- Register validators automatically by assembly, and return validation failures through the standard API error response format.

## API endpoints

Auth:

- POST /api/auth/register
- POST /api/auth/login
- GET /api/auth/me
- GET /api/public/ping
- GET /api/private/ping

Tasks:

- GET /api/tasks
- GET /api/tasks/{id}
- POST /api/tasks
- PUT /api/tasks/{id}
- DELETE /api/tasks/{id}

## Demo seed data

Seed demo user:

- Email: demo@example.com
- Password: Demo123!

Seed several demo tasks for the demo user.

## Database migrations

- Keep SQL migration scripts in an Infrastructure-layer migrations folder.
- Name migration scripts with dates or timestamps so database changes are tracked in order.
- Use raw SQL migration scripts instead of ORM migrations.
- At startup, the application should ensure the database exists.
- At startup, the application should ensure a migration history table exists.
- At startup, the application should run any migration script that has not already been recorded in the migration history table.
- Migration execution must be idempotent at the application level by tracking applied scripts.

## Testing expectations

Build and IDE validation:

- Keep project references explicit for every project that directly uses a type, including test projects. Do not rely on transitive references or previous restore state; Visual Studio design-time builds should compile cleanly after restore.
- Keep runtime package references explicit in the project that owns the concrete implementation. For example, Infrastructure password hashing must reference `Microsoft.Extensions.Identity.Core` directly because it instantiates ASP.NET Core `PasswordHasher`.
- Before calling backend work done, run `dotnet build backend\TaskBoard.slnx` from the repository root or `dotnet build TaskBoard.slnx` from `backend`.
- Then run `dotnet test` from `backend`.
- Before calling runtime or integration wiring done, run `docker compose up -d` from the repository root and smoke-test the affected service through the Compose-exposed port.

Unit tests:

- Task validation.
- Task ownership rules.
- Auth registration.
- Duplicate email.
- Login success.
- Login failure.

Integration tests:

- Repository CRUD.
- Auth API register/login.
- Protected endpoint rejects anonymous requests.
- Authenticated task CRUD.
- User cannot access another user's task.

## Frontend expectations

- React + TypeScript.
- Login page.
- Register page.
- Dashboard page.
- Task list.
- Create task.
- Edit task.
- Delete task.
- Simple task list behavior is enough for the first implementation.
- Tasks should support drag and drop between status columns.
- Loading states.
- Error states.
- Empty states.
- Responsive layout.
- Avoid browser console warnings where practical.

## Documentation expectations

README must include:

- Overview.
- User story.
- Architecture.
- Tech stack.
- Setup instructions.
- Seeded credentials.
- Backend commands.
- Frontend commands.
- Test commands.
- API endpoint summary.
- Known tradeoffs.

docs/AI_USAGE.md must include:

- Prompt used with Codex.
- Representative generated code.
- How the output was validated.
- What was corrected.
- Edge cases.
- Authentication decisions.
- Validation decisions.

Build docs/AI_USAGE.md incrementally as implementation phases land.

## Pull request expectations

Each PR must include:

- Clear scope.
- Tests or explanation of why no tests were needed.
- Updated documentation when behavior changes.
- Passing validation commands.
- No banned packages.
- No unrelated refactors.

## Definition of done

A task is done only when:

- The code builds.
- Relevant tests pass.
- The diff is scoped.
- The implementation follows Clean Architecture.
- No banned libraries were added.
- The README or docs are updated when needed.
- Docker Compose remains current and runnable for the full local application whenever runtime services or wiring change.
- A stale-artifact cleanup pass was completed: remove or update obsolete files, duplicate validation/business rules, unused manual-test assets, dead classes, and superseded docs created or made stale by the iteration.
