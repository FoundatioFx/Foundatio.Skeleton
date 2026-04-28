using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Foundatio.Skeleton.Tests;

public class StatusEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StatusEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetHealth_WhenCalled_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_WhenCalled_ReturnsOkWithVersionAndStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/status", TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ok", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("version", content, StringComparison.OrdinalIgnoreCase);
    }
}
