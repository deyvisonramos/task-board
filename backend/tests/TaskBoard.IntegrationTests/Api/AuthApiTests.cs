using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Api;

public sealed class AuthApiTests : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly TaskBoardApiFactory _factory;
    private readonly HttpClient _client;

    public AuthApiTests(PostgresFixture fixture)
    {
        _factory = new TaskBoardApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Register_ReturnsSuccess()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = UniqueEmail(),
            password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("user").GetProperty("email").GetString().Should().EndWith("@example.com");
        json.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.GetProperty("user").TryGetProperty("passwordHash", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Login_ReturnsJwt()
    {
        var credentials = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", credentials);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        var accessToken = json.RootElement.GetProperty("accessToken").GetString();

        accessToken.Should().NotBeNullOrWhiteSpace();
        accessToken!.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var credentials = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            credentials.Email,
            password = "WrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/private/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicEndpoint_WithoutToken_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/public/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_WithoutToken_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithToken_ReturnsSuccess()
    {
        var token = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/private/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUserWithoutPasswordHash()
    {
        var credentials = await RegisterUserAsync();
        var token = await LoginAsync(credentials);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("email").GetString().Should().Be(credentials.Email);
        json.RootElement.TryGetProperty("passwordHash", out _).Should().BeFalse();
    }

    [Fact]
    public async Task OpenApi_InDevelopment_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        return await LoginAsync(await RegisterUserAsync());
    }

    private async Task<Credentials> RegisterUserAsync()
    {
        var credentials = new Credentials(UniqueEmail(), "Password123!");

        var response = await _client.PostAsJsonAsync("/api/auth/register", credentials);
        response.EnsureSuccessStatusCode();

        return credentials;
    }

    private async Task<string> LoginAsync(Credentials credentials)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", credentials);
        response.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(response);

        return json.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login response did not include an access token.");
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

    private sealed record Credentials(string Email, string Password);
}
