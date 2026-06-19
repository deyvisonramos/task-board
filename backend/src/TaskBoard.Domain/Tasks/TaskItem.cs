namespace TaskBoard.Domain.Tasks;

public sealed class TaskItem
{
    public const int TitleMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

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
        Validate(id, userId, title, description, status, dueDate, createdAt, updatedAt);

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
        Validate(Id, UserId, title, description, status, dueDate, CreatedAt, updatedAt);

        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    private static void Validate(
        Guid id,
        Guid userId,
        string title,
        string? description,
        TaskItemStatus status,
        DateTime dueDate,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task id is required.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        if (title.Length > TitleMaxLength)
        {
            throw new ArgumentException("Task title is too long.", nameof(title));
        }

        if (description?.Length > DescriptionMaxLength)
        {
            throw new ArgumentException("Task description is too long.", nameof(description));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Task status is invalid.", nameof(status));
        }

        if (dueDate == default)
        {
            throw new ArgumentException("Task due date is required.", nameof(dueDate));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Task creation timestamp is required.", nameof(createdAt));
        }

        if (updatedAt == default)
        {
            throw new ArgumentException("Task update timestamp is required.", nameof(updatedAt));
        }
    }
}
