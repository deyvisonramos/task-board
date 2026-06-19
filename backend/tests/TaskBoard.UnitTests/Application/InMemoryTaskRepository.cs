using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.UnitTests.Application;

internal sealed class InMemoryTaskRepository : ITaskRepository
{
    public List<TaskItem> Items { get; } = [];

    public Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        Items.Add(taskItem);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(existingTask => existingTask.Id == taskItem.Id);
        return Task.CompletedTask;
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(task => task.Id == id));
    }

    public Task<IReadOnlyList<TaskItem>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TaskItem>>(
            Items.Where(task => task.UserId == userId).ToList());
    }

    public Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
