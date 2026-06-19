using TaskBoard.Domain.Tasks;

namespace TaskBoard.Application.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    TaskItemStatus Status = TaskItemStatus.Todo);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    TaskItemStatus Status);

public sealed record TaskDto(
    Guid Id,
    Guid UserId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
