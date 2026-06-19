using Microsoft.Extensions.Logging;
using Npgsql;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class DbInitializer
{
    private const string MigrationDirectoryName = "Migrations";

    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        DbConnectionFactory connectionFactory,
        ILogger<DbInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Database initialization started.");

            await EnsureDatabaseExistsAsync(cancellationToken);
            await EnsureMigrationHistoryTableAsync(cancellationToken);

            foreach (var scriptPath in GetMigrationScriptPaths())
            {
                await ExecuteScriptAsync(scriptPath, cancellationToken);
            }

            _logger.LogInformation("Database initialization completed.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Database initialization failed.");
            throw;
        }
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(_connectionFactory.ConnectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("PostgreSQL database name is required.");
        }

        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        if (await DatabaseExistsAsync(connection, databaseName, cancellationToken))
        {
            return;
        }

        _logger.LogInformation("Creating PostgreSQL database. DatabaseName: {DatabaseName}", databaseName);

        await using var command = connection.CreateCommand();
        command.CommandText = $"create database {QuoteIdentifier(databaseName)};";
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private async Task ExecuteScriptAsync(string scriptPath, CancellationToken cancellationToken)
    {
        var scriptName = Path.GetFileName(scriptPath);

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

        _logger.LogInformation("Applying database seed or migration script. ScriptName: {ScriptName}", scriptName);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await RecordScriptAppliedAsync(connection, transaction, scriptName, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Database seed or migration script applied. ScriptName: {ScriptName}", scriptName);
    }

    private static IReadOnlyList<string> GetMigrationScriptPaths()
    {
        var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Database", MigrationDirectoryName);

        if (!Directory.Exists(migrationsPath))
        {
            throw new DirectoryNotFoundException($"Database migrations folder '{migrationsPath}' was not found.");
        }

        return Directory
            .EnumerateFiles(migrationsPath, "*.sql", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<bool> DatabaseExistsAsync(
        NpgsqlConnection connection,
        string databaseName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists(
                select 1
                from pg_database
                where datname = @database_name
            );
            """;
        command.Parameters.AddWithValue("database_name", databaseName);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static async Task<bool> WasScriptAppliedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
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
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
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

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
