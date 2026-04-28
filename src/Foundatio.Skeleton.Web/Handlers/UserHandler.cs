using Foundatio.Mediator;
using Foundatio.Skeleton.Core.Models;
using Foundatio.Skeleton.Core.Models.Messages;
using Foundatio.Skeleton.Web.Messages;

namespace Foundatio.Skeleton.Web.Handlers;

[HandlerEndpointGroup("Users")]
public class UserHandler
{
    private static readonly Dictionary<string, User> s_users = new(StringComparer.OrdinalIgnoreCase);

    public Result<List<User>> Handle(GetUsers query) =>
        s_users.Values.ToList();

    public Result<User> Handle(GetUser query) =>
        s_users.TryGetValue(query.Id, out var user)
            ? user
            : Result.NotFound();

    public Task<(Result<User>, UserCreated?)> HandleAsync(
        CreateUser command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            FullName = command.FullName,
            EmailAddress = command.EmailAddress
        };

        s_users[user.Id] = user;

        return Task.FromResult<(Result<User>, UserCreated?)>(
            (Result<User>.Created(user), new UserCreated(user.Id, user.EmailAddress)));
    }

    public Task<(Result<User>, UserUpdated?)> HandleAsync(
        UpdateUser command, CancellationToken cancellationToken)
    {
        if (!s_users.TryGetValue(command.Id, out var user))
            return Task.FromResult<(Result<User>, UserUpdated?)>((Result.NotFound(), null));

        user.FullName = command.FullName ?? user.FullName;
        user.EmailAddress = command.EmailAddress ?? user.EmailAddress;
        user.UpdatedUtc = DateTimeOffset.UtcNow;

        return Task.FromResult<(Result<User>, UserUpdated?)>((user, new UserUpdated(user.Id)));
    }

    public Result Handle(DeleteUser command) =>
        s_users.Remove(command.Id) ? Result.Success() : Result.NotFound();
}
