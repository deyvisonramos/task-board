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
