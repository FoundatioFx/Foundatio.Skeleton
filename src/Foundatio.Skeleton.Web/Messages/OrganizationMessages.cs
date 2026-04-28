using System.ComponentModel.DataAnnotations;
using Foundatio.Mediator;
using Foundatio.Skeleton.Core.Models;

namespace Foundatio.Skeleton.Web.Messages;

public record CreateOrganization(
    [Required][StringLength(256, MinimumLength = 1)] string Name) : ICommand<Result<Organization>>;

public record DeleteOrganization([Required] string Id) : ICommand<Result>;

public record GetOrganizations() : IQuery<Result<List<Organization>>>;
public record GetOrganization([Required] string Id) : IQuery<Result<Organization>>;
