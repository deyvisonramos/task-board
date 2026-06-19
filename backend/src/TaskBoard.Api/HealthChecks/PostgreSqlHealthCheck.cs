using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Api.HealthChecks;

public sealed class PostgreSqlHealthCheck : IHealthCheck
{
    private readonly DbConnectionFactory _connectionFactory;

    public PostgreSqlHealthCheck(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL connection is available.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection is unavailable.", exception);
        }
    }
}
