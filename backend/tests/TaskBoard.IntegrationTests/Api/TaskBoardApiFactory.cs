using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.IntegrationTests.Api;

public sealed class TaskBoardApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _environment;

    public TaskBoardApiFactory(
        string connectionString,
        string environment = "Development")
    {
        _connectionString = connectionString;
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbConnectionFactory>();
            services.AddSingleton(new DbConnectionFactory(_connectionString));
        });
    }
}
