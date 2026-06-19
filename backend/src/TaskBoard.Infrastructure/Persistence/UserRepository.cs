using Npgsql;
using NpgsqlTypes;
using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select id, email, password_hash, created_at
            from users
            where id = @id;
            """;
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select id, email, password_hash, created_at
            from users
            where email = @email;
            """;
        command.Parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Text) { Value = email });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? MapUser(reader) : null;
    }

    public async Task<bool> AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            insert into users (id, email, password_hash, created_at)
            values (@id, @email, @password_hash, @created_at);
            """;

        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = user.Id });
        command.Parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Text) { Value = user.Email });
        command.Parameters.Add(new NpgsqlParameter("password_hash", NpgsqlDbType.Text) { Value = user.PasswordHash });
        command.Parameters.Add(new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz)
        {
            Value = ToUtc(user.CreatedAt)
        });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }
    }

    private static AppUser MapUser(NpgsqlDataReader reader)
    {
        return new AppUser(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetDateTime(3));
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}
