using Foundatio.Mediator;

namespace Foundatio.Skeleton.Web.Messages;

public record GetStatus() : IQuery<Result<StatusResponse>>;

public record StatusResponse(string Status, string Version, string MachineName);
