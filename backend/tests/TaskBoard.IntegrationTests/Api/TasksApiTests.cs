using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskBoard.Domain.Tasks;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Api;

public sealed class TasksApiTests : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly TaskBoardApiFactory _factory;
    private readonly HttpClient _client;

    public TasksApiTests(PostgresFixture fixture)
    {
        _factory = new TaskBoardApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithToken_CreatesTaskForAuthenticatedUser()
    {
        await AuthenticateAsync(_client);
        var currentUserId = await GetCurrentUserIdAsync(_client);
        var submittedUserId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            userId = submittedUserId,
            title = "Write Task API tests",
            description = "Cover create behavior",
            dueDate = DateTime.UtcNow.AddDays(1),
            status = nameof(TaskItemStatus.Todo)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("id").GetGuid().Should().NotBeEmpty();
        json.RootElement.GetProperty("userId").GetGuid().Should().Be(currentUserId);
        json.RootElement.GetProperty("userId").GetGuid().Should().NotBe(submittedUserId);
        json.RootElement.GetProperty("title").GetString().Should().Be("Write Task API tests");
    }

    [Fact]
    public async Task List_WithToken_ReturnsOnlyOwnTasks()
    {
        var firstUserClient = await CreateAuthenticatedClientAsync();
        var secondUserClient = await CreateAuthenticatedClientAsync();
        var firstUserTask = await CreateTaskAsync(firstUserClient, "First user task");
        var secondUserTask = await CreateTaskAsync(secondUserClient, "Second user task");

        var response = await firstUserClient.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        var ids = json.RootElement.EnumerateArray()
            .Select(task => task.GetProperty("id").GetGuid())
            .ToList();

        ids.Should().Contain(firstUserTask);
        ids.Should().NotContain(secondUserTask);
    }

    [Fact]
    public async Task Get_WithOwnTask_ReturnsTask()
    {
        await AuthenticateAsync(_client);
        var taskId = await CreateTaskAsync(_client, "Read task");

        var response = await _client.GetAsync($"/api/tasks/{taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("id").GetGuid().Should().Be(taskId);
        json.RootElement.GetProperty("title").GetString().Should().Be("Read task");
    }

    [Fact]
    public async Task Update_WithOwnTask_ReturnsUpdatedTask()
    {
        await AuthenticateAsync(_client);
        var taskId = await CreateTaskAsync(_client, "Original title");

        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", new
        {
            title = "Updated title",
            description = "Updated description",
            dueDate = DateTime.UtcNow.AddDays(2),
            status = nameof(TaskItemStatus.InProgress)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("id").GetGuid().Should().Be(taskId);
        json.RootElement.GetProperty("title").GetString().Should().Be("Updated title");
        json.RootElement.GetProperty("description").GetString().Should().Be("Updated description");
        json.RootElement.GetProperty("status").GetString().Should().Be(nameof(TaskItemStatus.InProgress));
    }

    [Fact]
    public async Task Delete_WithOwnTask_RemovesTask()
    {
        await AuthenticateAsync(_client);
        var taskId = await CreateTaskAsync(_client, "Delete task");

        var response = await _client.DeleteAsync($"/api/tasks/{taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/tasks/{taskId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_WithAnotherUsersTask_ReturnsNotFound()
    {
        var firstUserClient = await CreateAuthenticatedClientAsync();
        var secondUserClient = await CreateAuthenticatedClientAsync();
        var secondUserTask = await CreateTaskAsync(secondUserClient, "Second user task");

        var response = await firstUserClient.GetAsync($"/api/tasks/{secondUserTask}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithAnotherUsersTask_ReturnsNotFound()
    {
        var firstUserClient = await CreateAuthenticatedClientAsync();
        var secondUserClient = await CreateAuthenticatedClientAsync();
        var secondUserTask = await CreateTaskAsync(secondUserClient, "Second user task");

        var response = await firstUserClient.PutAsJsonAsync($"/api/tasks/{secondUserTask}", new
        {
            title = "Cross-user update",
            description = "Should not apply",
            dueDate = DateTime.UtcNow.AddDays(2),
            status = nameof(TaskItemStatus.Done)
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerResponse = await secondUserClient.GetAsync($"/api/tasks/{secondUserTask}");
        var json = await ReadJsonAsync(ownerResponse);
        json.RootElement.GetProperty("title").GetString().Should().Be("Second user task");
    }

    [Fact]
    public async Task Delete_WithAnotherUsersTask_ReturnsNotFound()
    {
        var firstUserClient = await CreateAuthenticatedClientAsync();
        var secondUserClient = await CreateAuthenticatedClientAsync();
        var secondUserTask = await CreateTaskAsync(secondUserClient, "Second user task");

        var response = await firstUserClient.DeleteAsync($"/api/tasks/{secondUserTask}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerResponse = await secondUserClient.GetAsync($"/api/tasks/{secondUserTask}");
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithInvalidTitle_ReturnsBadRequest()
    {
        await AuthenticateAsync(_client);

        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = " ",
            description = "Invalid title",
            dueDate = DateTime.UtcNow.AddDays(1),
            status = nameof(TaskItemStatus.Todo)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("Validation.Failed");
        json.RootElement.GetProperty("validation")
            .EnumerateArray()
            .Should()
            .Contain(item => item.GetProperty("code").GetString() == "Task.TitleRequired");
    }

    [Fact]
    public async Task Create_WithInvalidStatus_ReturnsValidationResponse()
    {
        await AuthenticateAsync(_client);
        using var content = new StringContent(
            """
            {
              "title": "Invalid status task",
              "description": null,
              "dueDate": "2026-01-01T00:00:00Z",
              "status": 999
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/tasks", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("Validation.Failed");
        json.RootElement.GetProperty("validation")
            .EnumerateArray()
            .Should()
            .Contain(item => item.GetProperty("code").GetString() == "Validation.Invalid");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await AuthenticateAsync(client);

        return client;
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var token = await RegisterAndLoginAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        var email = UniqueEmail();
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });
        loginResponse.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(loginResponse);

        return json.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login response did not include an access token.");
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title,
            description = "Created by integration test",
            dueDate = DateTime.UtcNow.AddDays(1),
            status = nameof(TaskItemStatus.Todo)
        });
        response.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(response);

        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> GetCurrentUserIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(response);

        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();

        return await JsonDocument.ParseAsync(stream);
    }

    private static string UniqueEmail()
    {
        return $"{Guid.NewGuid():N}@example.com";
    }
}
