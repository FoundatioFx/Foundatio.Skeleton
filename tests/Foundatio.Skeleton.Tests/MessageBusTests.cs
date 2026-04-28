using Foundatio.Messaging;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Foundatio.Skeleton.Tests;

public class MessageBusTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MessageBusTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PublishAsync_WithSubscriber_DeliversMessageToSubscriber()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var tcs = new TaskCompletionSource<TestMessage>();
        var expected = new TestMessage("hello", 42);

        await messageBus.SubscribeAsync<TestMessage>(msg =>
        {
            tcs.TrySetResult(msg);
        }, TestContext.Current.CancellationToken);

        // Act
        await messageBus.PublishAsync(expected, cancellationToken: TestContext.Current.CancellationToken);
        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected.Text, received.Text);
        Assert.Equal(expected.Number, received.Number);
    }

    public record TestMessage(string Text, int Number);
}
