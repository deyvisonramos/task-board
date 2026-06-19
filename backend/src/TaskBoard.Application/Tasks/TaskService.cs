using TaskBoard.Application.Common;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.Application.Tasks;

public sealed class TaskService : ITaskService
{
    public const int TitleMaxLength = TaskItem.TitleMaxLength;
    public const int DescriptionMaxLength = TaskItem.DescriptionMaxLength;

    private readonly ITaskRepository _tasks;

    public TaskService(ITaskRepository tasks)
    {
        _tasks = tasks;
    }

    public async Task<Result<IReadOnlyList<TaskDto>>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _tasks.ListByUserIdAsync(userId, cancellationToken);

        return Result<IReadOnlyList<TaskDto>>.Success(tasks.Select(ToDto).ToList());
    }

    public async Task<Result<TaskDto>> GetByIdAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(taskId, cancellationToken);

        if (task is null || task.UserId != userId)
        {
            return Result<TaskDto>.Failure("Task.NotFound", "Task was not found.");
        }

        return Result<TaskDto>.Success(ToDto(task));
    }

    public async Task<Result<TaskDto>> CreateAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(request.Title, request.Description, request.DueDate, request.Status);

        if (validationErrors.Count > 0)
        {
            return Result<TaskDto>.ValidationFailure(validationErrors);
        }

        var now = DateTime.UtcNow;
        var task = new TaskItem(
            Guid.NewGuid(),
            userId,
            NormalizeRequiredText(request.Title),
            NormalizeOptionalText(request.Description),
            request.Status,
            NormalizeUtc(request.DueDate!.Value),
            now,
            now);

        await _tasks.AddAsync(task, cancellationToken);

        return Result<TaskDto>.Success(ToDto(task));
    }

    public async Task<Result<TaskDto>> UpdateAsync(
        Guid userId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(request.Title, request.Description, request.DueDate, request.Status);

        if (validationErrors.Count > 0)
        {
            return Result<TaskDto>.ValidationFailure(validationErrors);
        }

        var task = await _tasks.GetByIdAsync(taskId, cancellationToken);

        if (task is null || task.UserId != userId)
        {
            return Result<TaskDto>.Failure("Task.NotFound", "Task was not found.");
        }

        task.Update(
            NormalizeRequiredText(request.Title),
            NormalizeOptionalText(request.Description),
            request.Status,
            NormalizeUtc(request.DueDate!.Value),
            DateTime.UtcNow);

        await _tasks.UpdateAsync(task, cancellationToken);

        return Result<TaskDto>.Success(ToDto(task));
    }

    public async Task<Result> DeleteAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(taskId, cancellationToken);

        if (task is null || task.UserId != userId)
        {
            return Result.Failure("Task.NotFound", "Task was not found.");
        }

        await _tasks.DeleteAsync(task, cancellationToken);

        return Result.Success();
    }

    private static List<ValidationError> Validate(
        string title,
        string? description,
        DateTime? dueDate,
        TaskItemStatus status = TaskItemStatus.Todo)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new ValidationError("Task.TitleRequired", "Title is required."));
        }
        else if (title.Trim().Length > TitleMaxLength)
        {
            errors.Add(new ValidationError("Task.TitleTooLong", "Title must be 100 characters or fewer."));
        }

        if (description is not null && description.Length > DescriptionMaxLength)
        {
            errors.Add(new ValidationError(
                "Task.DescriptionTooLong",
                "Description must be 1000 characters or fewer."));
        }

        if (dueDate is null)
        {
            errors.Add(new ValidationError("Task.DueDateRequired", "Due date is required."));
        }

        if (!Enum.IsDefined(status))
        {
            errors.Add(new ValidationError("Task.StatusInvalid", "Status is invalid."));
        }

        return errors;
    }

    private static TaskDto ToDto(TaskItem task)
    {
        return new TaskDto(
            task.Id,
            task.UserId,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt);
    }

    private static string NormalizeRequiredText(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }
}
