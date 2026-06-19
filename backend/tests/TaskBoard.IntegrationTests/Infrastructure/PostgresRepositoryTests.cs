using FluentAssertions;
using Npgsql;
using TaskBoard.Domain.Tasks;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TaskBoard.IntegrationTests.Infrastructure;

public sealed class PostgresRepositoryTests : IClassFixture<PostgresRepositoryTests.PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateUserAndGetByEmailAsync_ReturnsCreatedUser()
    {
        var users = _fixture.CreateUserRepository();
        var user = NewUser();

        await users.AddAsync(user);

        var result = await users.GetByEmailAsync(user.Email);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.PasswordHash.Should().Be(user.PasswordHash);
        result.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task AddUserAsync_WithDuplicateEmail_ThrowsUniqueViolation()
    {
        var users = _fixture.CreateUserRepository();
        var email = UniqueEmail();

        await users.AddAsync(NewUser(email: email));

        var action = async () => await users.AddAsync(NewUser(email: email));

        var exception = await action.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task AddTaskAndGetByIdAsync_ReturnsCreatedTask()
    {
        var users = _fixture.CreateUserRepository();
        var tasks = _fixture.CreateTaskRepository();
        var user = NewUser();
        var task = NewTask(user.Id);

        await users.AddAsync(user);
        await tasks.AddAsync(task);

        var result = await tasks.GetByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
        result.UserId.Should().Be(user.Id);
        result.Title.Should().Be(task.Title);
        result.Description.Should().Be(task.Description);
        result.Status.Should().Be(task.Status);
        result.DueDate.Date.Should().Be(task.DueDate.Date);
        result.CreatedAt.Should().BeCloseTo(task.CreatedAt, TimeSpan.FromMilliseconds(1));
        result.UpdatedAt.Should().BeCloseTo(task.UpdatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task ListByUserIdAsync_ReturnsOnlyThatUsersTasks()
    {
        var users = _fixture.CreateUserRepository();
        var tasks = _fixture.CreateTaskRepository();
        var firstUser = NewUser();
        var secondUser = NewUser();
        var firstTask = NewTask(firstUser.Id, title: "First user task");
        var secondTask = NewTask(secondUser.Id, title: "Second user task");

        await users.AddAsync(firstUser);
        await users.AddAsync(secondUser);
        await tasks.AddAsync(firstTask);
        await tasks.AddAsync(secondTask);

        var result = await tasks.ListByUserIdAsync(firstUser.Id);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(firstTask.Id);
        result.Should().NotContain(task => task.UserId == secondUser.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsTaskChanges()
    {
        var users = _fixture.CreateUserRepository();
        var tasks = _fixture.CreateTaskRepository();
        var user = NewUser();
        var task = NewTask(user.Id);
        var updatedAt = DateTime.UtcNow.AddMinutes(5);

        await users.AddAsync(user);
        await tasks.AddAsync(task);

        task.Update(
            "Updated title",
            "Updated description",
            TaskItemStatus.Done,
            DateTime.UtcNow.Date.AddDays(3),
            updatedAt);

        await tasks.UpdateAsync(task);

        var result = await tasks.GetByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated title");
        result.Description.Should().Be("Updated description");
        result.Status.Should().Be(TaskItemStatus.Done);
        result.DueDate.Date.Should().Be(task.DueDate.Date);
        result.UpdatedAt.Should().BeCloseTo(updatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTask()
    {
        var users = _fixture.CreateUserRepository();
        var tasks = _fixture.CreateTaskRepository();
        var user = NewUser();
        var task = NewTask(user.Id);

        await users.AddAsync(user);
        await tasks.AddAsync(task);

        await tasks.DeleteAsync(task);

        var result = await tasks.GetByIdAsync(task.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_WhenRunAgain_DoesNotReapplyScripts()
    {
        var initializer = _fixture.CreateInitializer();

        var action = async () => await initializer.InitializeAsync();

        await action.Should().NotThrowAsync();
    }

    private static AppUser NewUser(string? email = null)
    {
        return new AppUser(
            Guid.NewGuid(),
            email ?? UniqueEmail(),
            "password-hash",
            DateTime.UtcNow);
    }

    private static TaskItem NewTask(Guid userId, string title = "Test task")
    {
        var now = DateTime.UtcNow;

        return new TaskItem(
            Guid.NewGuid(),
            userId,
            title,
            "Test description",
            TaskItemStatus.Todo,
            now.Date.AddDays(1),
            now,
            now);
    }

    private static string UniqueEmail()
    {
        return $"{Guid.NewGuid():N}@example.com";
    }

    public sealed class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .Build();

        private DbConnectionFactory? _connectionFactory;

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            _connectionFactory = new DbConnectionFactory(_postgres.GetConnectionString());
            var initializer = new DbInitializer(_connectionFactory);

            await initializer.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        public PostgresUserRepository CreateUserRepository()
        {
            return new PostgresUserRepository(ConnectionFactory);
        }

        public PostgresTaskRepository CreateTaskRepository()
        {
            return new PostgresTaskRepository(ConnectionFactory);
        }

        public DbInitializer CreateInitializer()
        {
            return new DbInitializer(ConnectionFactory);
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
}
