using FluentAssertions;

namespace TaskBoard.IntegrationTests.Api;

public sealed class ApiConventionTests
{
    [Fact]
    public void ApiControllerBase_DoesNotMapAuthenticationFailures()
    {
        var source = File.ReadAllText(FindSourceFile(
            "src",
            "TaskBoard.Api",
            "Controllers",
            "ApiControllerBase.cs"));

        source.Should().NotContain("Unauthorized(");
        source.Should().NotContain("\"Auth.");
    }

    [Fact]
    public void Program_RegistersFallbackAuthorizationPolicy()
    {
        var source = File.ReadAllText(FindSourceFile(
            "src",
            "TaskBoard.Api",
            "Program.cs"));

        source.Should().Contain("FallbackPolicy");
        source.Should().Contain("RequireAuthenticatedUser");
    }

    private static string FindSourceFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. pathSegments]);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find source file '{Path.Combine(pathSegments)}' from '{AppContext.BaseDirectory}'.");
    }
}
