namespace TaskBoard.Infrastructure.Persistence;

public sealed class DbInitializer
{
    private static readonly string[] ScriptNames =
    [
        "schema.sql",
        "seed.sql"
    ];

    private readonly DbConnectionFactory _connectionFactory;

    public DbInitializer(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseExistsAsync(cancellationToken);
        await EnsureMigrationHistoryTableAsync(cancellationToken);

        foreach (var scriptName in ScriptNames)
        {
            await ExecuteScriptAsync(scriptName, cancellationToken);
        }
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
    }

    private async Task EnsureMigrationHistoryTableAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            create table if not exists migration_history
            (
                script_name text primary key,
                applied_at timestamptz not null
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteScriptAsync(string scriptName, CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Database", scriptName);

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Database script '{scriptName}' was not found.", scriptPath);
        }

        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (await WasScriptAppliedAsync(connection, transaction, scriptName, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await RecordScriptAppliedAsync(connection, transaction, scriptName, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> WasScriptAppliedAsync(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction transaction,
        string scriptName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            select exists(
                select 1
                from migration_history
                where script_name = @script_name
            );
            """;
        command.Parameters.AddWithValue("script_name", scriptName);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static async Task RecordScriptAppliedAsync(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction transaction,
        string scriptName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            insert into migration_history (script_name, applied_at)
            values (@script_name, @applied_at);
            """;
        command.Parameters.AddWithValue("script_name", scriptName);
        command.Parameters.AddWithValue("applied_at", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
