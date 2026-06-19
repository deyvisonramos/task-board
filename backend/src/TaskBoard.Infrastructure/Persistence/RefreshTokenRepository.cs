using Npgsql;
using NpgsqlTypes;
using TaskBoard.Application.Auth;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            insert into refresh_tokens
                (id, user_id, token_hash, expires_at, created_at, revoked_at, replaced_by_token_hash)
            values
                (@id, @user_id, @token_hash, @expires_at, @created_at, @revoked_at, @replaced_by_token_hash);
            """;

        AddParameters(command, refreshToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            select id, user_id, token_hash, expires_at, created_at, revoked_at, replaced_by_token_hash
            from refresh_tokens
            where token_hash = @token_hash;
            """;
        command.Parameters.Add(new NpgsqlParameter("token_hash", NpgsqlDbType.Text) { Value = tokenHash });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? MapRefreshToken(reader) : null;
    }

    public async Task RevokeAsync(
        Guid id,
        DateTime revokedAt,
        string? replacedByTokenHash,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            update refresh_tokens
            set revoked_at = @revoked_at,
                replaced_by_token_hash = @replaced_by_token_hash
            where id = @id;
            """;
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = id });
        command.Parameters.Add(new NpgsqlParameter("revoked_at", NpgsqlDbType.TimestampTz)
        {
            Value = ToUtc(revokedAt)
        });
        command.Parameters.Add(new NpgsqlParameter("replaced_by_token_hash", NpgsqlDbType.Text)
        {
            Value = (object?)replacedByTokenHash ?? DBNull.Value
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(NpgsqlCommand command, RefreshToken refreshToken)
    {
        command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Uuid) { Value = refreshToken.Id });
        command.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = refreshToken.UserId });
        command.Parameters.Add(new NpgsqlParameter("token_hash", NpgsqlDbType.Text) { Value = refreshToken.TokenHash });
        command.Parameters.Add(new NpgsqlParameter("expires_at", NpgsqlDbType.TimestampTz)
        {
            Value = ToUtc(refreshToken.ExpiresAt)
        });
        command.Parameters.Add(new NpgsqlParameter("created_at", NpgsqlDbType.TimestampTz)
        {
            Value = ToUtc(refreshToken.CreatedAt)
        });
        command.Parameters.Add(new NpgsqlParameter("revoked_at", NpgsqlDbType.TimestampTz)
        {
            Value = (object?)refreshToken.RevokedAt ?? DBNull.Value
        });
        command.Parameters.Add(new NpgsqlParameter("replaced_by_token_hash", NpgsqlDbType.Text)
        {
            Value = (object?)refreshToken.ReplacedByTokenHash ?? DBNull.Value
        });
    }

    private static RefreshToken MapRefreshToken(NpgsqlDataReader reader)
    {
        return new RefreshToken(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc),
            DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
            reader.IsDBNull(5) ? null : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
            reader.IsDBNull(6) ? null : reader.GetString(6));
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}
