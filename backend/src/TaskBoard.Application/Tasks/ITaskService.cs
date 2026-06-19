using TaskBoard.Application.Common;

namespace TaskBoard.Application.Tasks;

public interface ITaskService
{
    Task<Result<IReadOnlyList<TaskDto>>> ListAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<TaskDto>> GetByIdAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

    Task<Result<TaskDto>> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<Result<TaskDto>> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);
}
