using FluentAssertions;
using Npgsql;
using TaskBoard.Domain.Tasks;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests.Infrastructure;

public sealed class RepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public RepositoryTests(PostgresFixture fixture)
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
    public async Task AddUserAsync_WithDuplicateEmail_ReturnsFalse()
    {
        var users = _fixture.CreateUserRepository();
        var email = UniqueEmail();

        var firstResult = await users.AddAsync(NewUser(email: email));

        var secondResult = await users.AddAsync(NewUser(email: email));

        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse();
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
        result.DueDate.Should().BeCloseTo(task.DueDate, TimeSpan.FromMilliseconds(1));
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
            DateTime.UtcNow.AddDays(3),
            updatedAt);

        await tasks.UpdateAsync(task);

        var result = await tasks.GetByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated title");
        result.Description.Should().Be("Updated description");
        result.Status.Should().Be(TaskItemStatus.Done);
        result.DueDate.Should().BeCloseTo(task.DueDate, TimeSpan.FromMilliseconds(1));
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
    public async Task RefreshTokenRepository_AddGetAndRevoke_PersistsTokenState()
    {
        var users = _fixture.CreateUserRepository();
        var refreshTokens = _fixture.CreateRefreshTokenRepository();
        var user = NewUser();
        var token = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            $"hash-{Guid.NewGuid():N}",
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow);

        await users.AddAsync(user);
        await refreshTokens.AddAsync(token);

        var result = await refreshTokens.GetByTokenHashAsync(token.TokenHash);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.IsActive(DateTime.UtcNow).Should().BeTrue();

        await refreshTokens.RevokeAsync(token.Id, DateTime.UtcNow, "replacement-hash");

        var revoked = await refreshTokens.GetByTokenHashAsync(token.TokenHash);

        revoked.Should().NotBeNull();
        revoked!.IsActive(DateTime.UtcNow).Should().BeFalse();
        revoked.ReplacedByTokenHash.Should().Be("replacement-hash");
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
            now.AddDays(1),
            now,
            now);
    }

    private static string UniqueEmail()
    {
        return $"{Guid.NewGuid():N}@example.com";
    }
}
