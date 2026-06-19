namespace TaskBoard.Domain.Tasks;

public sealed class TaskItem
{
    public TaskItem(
        Guid id,
        Guid userId,
        string title,
        string? description,
        TaskItemStatus status,
        DateTime dueDate,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    public void Update(
        string title,
        string? description,
        TaskItemStatus status,
        DateTime dueDate,
        DateTime updatedAt)
    {
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }
}
