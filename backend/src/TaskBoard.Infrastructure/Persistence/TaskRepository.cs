using Npgsql;
using NpgsqlTypes;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class TaskRepository : ITaskRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public TaskRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TaskItem>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select id, user_id, title, description, status, due_date, created_at, updated_at
            from tasks
            where user_id = @user_id
            order by created_at, id;
            """;
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tasks = new List<TaskItem>();

        while (await reader.ReadAsync(cancellationToken))
        {
            tasks.Add(MapTask(reader));
        }

        return tasks;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select id, user_id, title, description, status, due_date, created_at, updated_at
            from tasks
            where id = @id;
            """;
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? MapTask(reader) : null;
    }

    public async Task AddAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            insert into tasks (id, user_id, title, description, status, due_date, created_at, updated_at)
            values (@id, @user_id, @title, @description, @status, @due_date, @created_at, @updated_at);
            """;

        AddInsertParameters(command, taskItem);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            update tasks
            set title = @title,
                description = @description,
                status = @status,
                due_date = @due_date,
                updated_at = @updated_at
            where id = @id;
            """;

        AddUpdateParameters(command, taskItem);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            delete from tasks
            where id = @id;
            """;
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = taskItem.Id });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddInsertParameters(NpgsqlCommand command, TaskItem taskItem)
    {
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = taskItem.Id });
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = taskItem.UserId });
        AddEditableTaskParameters(command, taskItem);
        command.Parameters.Add(new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz)
        {
            Value = ToUtc(taskItem.CreatedAt)
        });
    }

    private static void AddUpdateParameters(NpgsqlCommand command, TaskItem taskItem)
    {
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = taskItem.Id });
        AddEditableTaskParameters(command, taskItem);
    }

    private static void AddEditableTaskParameters(NpgsqlCommand command, TaskItem taskItem)
    {
        command.Parameters.Add(new NpgsqlParameter("title", NpgsqlDbType.Varchar) { Value = taskItem.Title });
        command.Parameters.Add(new NpgsqlParameter("description", NpgsqlDbType.Varchar)
        {
            Value = (object?)taskItem.Description ?? DBNull.Value
        });
        command.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Varchar)
        {
            Value = taskItem.Status.ToString()
        });
        command.Parameters.Add(new NpgsqlParameter("due_date", NpgsqlDbType.Date)
        {
            Value = taskItem.DueDate.Date
        });
        command.Parameters.Add(new NpgsqlParameter("updated_at", NpgsqlDbType.TimestampTz)
        {
            Value = ToUtc(taskItem.UpdatedAt)
        });
    }

    private static TaskItem MapTask(NpgsqlDataReader reader)
    {
        var createdAt = reader.GetDateTime(6);
        var updatedAt = reader.IsDBNull(7) ? createdAt : reader.GetDateTime(7);

        return new TaskItem(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            Enum.Parse<TaskItemStatus>(reader.GetString(4)),
            DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
            createdAt,
            updatedAt);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}
