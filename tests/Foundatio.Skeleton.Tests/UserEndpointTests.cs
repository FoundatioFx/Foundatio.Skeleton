using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Foundatio.Skeleton.Tests;

public class UserEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UserEndpointTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task PostUser_WithValidData_ReturnsCreatedAndGetReturnsUser()
    {
        // Arrange
        var request = new { FullName = "Test User", EmailAddress = "test@localhost" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request, TestContext.Current.CancellationToken);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(user);
        Assert.Equal("Test User", user.FullName);

        var getResponse = await _client.GetAsync($"/api/users/{user.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetUser_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = "nonexistent-id";

        // Act
        var response = await _client.GetAsync($"/api/users/{nonExistentId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record UserResponse(string Id, string FullName, string EmailAddress);
}
