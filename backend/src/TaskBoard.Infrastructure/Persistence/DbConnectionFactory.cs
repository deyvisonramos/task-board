using Npgsql;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class DbConnectionFactory
{
    public DbConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A PostgreSQL connection string is required.", nameof(connectionString));
        }

        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        return connection;
    }
}
