using Microsoft.Extensions.Logging.Abstractions;
using TaskBoard.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TaskBoard.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private DbConnectionFactory? _connectionFactory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _connectionFactory = new DbConnectionFactory(_postgres.GetConnectionString());
        var initializer = new DbInitializer(_connectionFactory, NullLogger<DbInitializer>.Instance);

        await initializer.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public UserRepository CreateUserRepository()
    {
        return new UserRepository(ConnectionFactory);
    }

    public TaskRepository CreateTaskRepository()
    {
        return new TaskRepository(ConnectionFactory);
    }

    public string ConnectionString => ConnectionFactory.ConnectionString;

    public DbInitializer CreateInitializer()
    {
        return new DbInitializer(ConnectionFactory, NullLogger<DbInitializer>.Instance);
    }

    private DbConnectionFactory ConnectionFactory
    {
        get
        {
            if (_connectionFactory is null)
            {
                throw new InvalidOperationException("PostgreSQL fixture has not been initialized.");
            }

            return _connectionFactory;
        }
    }
}
