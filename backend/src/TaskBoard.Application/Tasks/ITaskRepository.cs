using TaskBoard.Domain.Tasks;

namespace TaskBoard.Application.Tasks;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken = default);

    Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default);

    Task DeleteAsync(TaskItem taskItem, CancellationToken cancellationToken = default);
}
