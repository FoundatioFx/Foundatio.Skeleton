using System.ComponentModel.DataAnnotations;
using Foundatio.Mediator;
using Foundatio.Skeleton.Core.Models;

namespace Foundatio.Skeleton.Web.Messages;

public record CreateUser(
    [Required][StringLength(256, MinimumLength = 1)] string FullName,
    [Required][EmailAddress][StringLength(256)] string EmailAddress) : ICommand<Result<User>>;

public record UpdateUser(
    [Required] string Id,
    string? FullName,
    string? EmailAddress) : ICommand<Result<User>>;

public record DeleteUser([Required] string Id) : ICommand<Result>;

public record GetUsers() : IQuery<Result<List<User>>>;
public record GetUser([Required] string Id) : IQuery<Result<User>>;
