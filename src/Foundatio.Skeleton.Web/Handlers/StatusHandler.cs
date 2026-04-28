using System.Reflection;
using Foundatio.Mediator;
using Foundatio.Skeleton.Web.Messages;

namespace Foundatio.Skeleton.Web.Handlers;

[HandlerEndpointGroup("Status")]
public class StatusHandler
{
    public Result<StatusResponse> Handle(GetStatus query)
    {
        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "dev";

        return new StatusResponse("ok", version, Environment.MachineName);
    }
}
