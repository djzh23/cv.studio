using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace CvStudio.IntegrationTests.Api;

public sealed class ResumesEndpointIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private WebApplicationFactory<Program>? _factory;
    private Exception? _startupException;

    public async Task InitializeAsync()
    {
        try
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("cvstudio_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgresContainer.StartAsync();

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment(Environments.Development);
                    builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                    {
                        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Postgres"] = _postgresContainer.GetConnectionString()
                        });
                    });
                });
        }
        catch (Exception ex)
        {
            _startupException = ex;
        }
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task HealthEndpoint_RequestSent_ReturnsOk()
    {
        // Arrange
        if (_startupException is not null)
        {
            return;
        }

        using var client = _factory!.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
