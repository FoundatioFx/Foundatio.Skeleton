using Foundatio.Mediator;
using Foundatio.Skeleton.Core.Models;
using Foundatio.Skeleton.Core.Models.Messages;
using Foundatio.Skeleton.Web.Messages;

namespace Foundatio.Skeleton.Web.Handlers;

[HandlerEndpointGroup("Organizations")]
public class OrganizationHandler
{
    private static readonly Dictionary<string, Organization> s_organizations = new(StringComparer.OrdinalIgnoreCase);

    public Result<List<Organization>> Handle(GetOrganizations query) =>
        s_organizations.Values.ToList();

    public Result<Organization> Handle(GetOrganization query) =>
        s_organizations.TryGetValue(query.Id, out var organization)
            ? organization
            : Result.NotFound();

    public Task<(Result<Organization>, OrganizationCreated?)> HandleAsync(
        CreateOrganization command, CancellationToken cancellationToken)
    {
        var organization = new Organization { Name = command.Name };
        s_organizations[organization.Id] = organization;

        return Task.FromResult<(Result<Organization>, OrganizationCreated?)>(
            (Result<Organization>.Created(organization), new OrganizationCreated(organization.Id, organization.Name)));
    }

    public Result Handle(DeleteOrganization command) =>
        s_organizations.Remove(command.Id) ? Result.Success() : Result.NotFound();
}
