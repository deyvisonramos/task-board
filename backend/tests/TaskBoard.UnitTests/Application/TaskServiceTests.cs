using FluentAssertions;
using TaskBoard.Application.Tasks;
using TaskBoard.Domain.Tasks;

namespace TaskBoard.UnitTests.Application;

public sealed class TaskServiceTests
{
    private readonly InMemoryTaskRepository _tasks = new();
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_tasks);
    }

    [Fact]
    public async Task CreateAsync_WithValidInput_CreatesTask()
    {
        var userId = Guid.NewGuid();
        var request = new CreateTaskRequest(
            "Write tests",
            "Cover the task service",
            DateTime.UtcNow.AddDays(1),
            TaskItemStatus.Todo);

        var result = await _service.CreateAsync(userId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Write tests");
        result.Value.UserId.Should().Be(userId);
        _tasks.Items.Should().ContainSingle(task => task.Id == result.Value.Id);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyTitle_Fails()
    {
        var request = new CreateTaskRequest(" ", null, DateTime.UtcNow.AddDays(1));

        var result = await _service.CreateAsync(Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(error => error.Code == "Task.TitleRequired");
        _tasks.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithLongTitle_Fails()
    {
        var request = new CreateTaskRequest(new string('a', 101), null, DateTime.UtcNow.AddDays(1));

        var result = await _service.CreateAsync(Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(error => error.Code == "Task.TitleTooLong");
        _tasks.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithLongDescription_Fails()
    {
        var request = new CreateTaskRequest("Title", new string('a', 1001), DateTime.UtcNow.AddDays(1));

        var result = await _service.CreateAsync(Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(error => error.Code == "Task.DescriptionTooLong");
        _tasks.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithInvalidStatus_Fails()
    {
        var request = new CreateTaskRequest(
            "Title",
            null,
            DateTime.UtcNow.AddDays(1),
            (TaskItemStatus)999);

        var result = await _service.CreateAsync(Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(error => error.Code == "Task.StatusInvalid");
        _tasks.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ForOwnTask_Succeeds()
    {
        var userId = Guid.NewGuid();
        var task = NewTask(userId);
        _tasks.Items.Add(task);

        var request = new UpdateTaskRequest(
            "Updated",
            "Updated description",
            DateTime.UtcNow.AddDays(2),
            TaskItemStatus.InProgress);

        var result = await _service.UpdateAsync(userId, task.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Updated");
        result.Value.Description.Should().Be("Updated description");
        result.Value.Status.Should().Be(TaskItemStatus.InProgress);
    }

    [Fact]
    public async Task UpdateAsync_ForAnotherUsersTask_Fails()
    {
        var task = NewTask(Guid.NewGuid());
        _tasks.Items.Add(task);
        var originalTitle = task.Title;

        var request = new UpdateTaskRequest(
            "Updated",
            "Updated description",
            DateTime.UtcNow.AddDays(2),
            TaskItemStatus.Done);

        var result = await _service.UpdateAsync(Guid.NewGuid(), task.Id, request);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Task.NotFound");
        task.Title.Should().Be(originalTitle);
    }

    [Fact]
    public async Task DeleteAsync_ForOwnTask_Succeeds()
    {
        var userId = Guid.NewGuid();
        var task = NewTask(userId);
        _tasks.Items.Add(task);

        var result = await _service.DeleteAsync(userId, task.Id);

        result.IsSuccess.Should().BeTrue();
        _tasks.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ForAnotherUsersTask_Fails()
    {
        var task = NewTask(Guid.NewGuid());
        _tasks.Items.Add(task);

        var result = await _service.DeleteAsync(Guid.NewGuid(), task.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Task.NotFound");
        _tasks.Items.Should().ContainSingle(existingTask => existingTask.Id == task.Id);
    }

    private static TaskItem NewTask(Guid userId)
    {
        var now = DateTime.UtcNow;

        return new TaskItem(
            Guid.NewGuid(),
            userId,
            "Original",
            "Original description",
            TaskItemStatus.Todo,
            now.AddDays(1),
            now,
            now);
    }

}
