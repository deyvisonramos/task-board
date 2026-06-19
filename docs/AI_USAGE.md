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
- `due_date` follows the requested PostgreSQL `date` column type.
