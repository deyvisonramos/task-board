using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskBoard.Api.Middleware;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Api;

public sealed class ObservabilityApiTests : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly TaskBoardApiFactory _factory;
    private readonly HttpClient _client;

    public ObservabilityApiTests(PostgresFixture fixture)
    {
        _factory = new TaskBoardApiFactory(fixture.ConnectionString, "Testing");
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Response_IncludesCorrelationId_WhenRequestDoesNotProvideOne()
    {
        var response = await _client.GetAsync("/api/public/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values)
            .Should()
            .BeTrue();
        values!.Single().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Response_PreservesCorrelationId_WhenRequestProvidesOne()
    {
        const string correlationId = "test-correlation-id";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/ping");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)
            .Single()
            .Should()
            .Be(correlationId);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_StillReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/private/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnhandledException_ReturnsStandardErrorResponseWithCorrelationHeader()
    {
        const string correlationId = "error-correlation-id";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test/unhandled-error");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("code").GetString().Should().Be("Unexpected.Error");
        json.RootElement.GetProperty("message").GetString().Should().Be("The request failed unexpectedly.");
        json.RootElement.GetProperty("validation").EnumerateArray().Should().BeEmpty();
        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)
            .Single()
            .Should()
            .Be(correlationId);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();

        return await JsonDocument.ParseAsync(stream);
    }
}
