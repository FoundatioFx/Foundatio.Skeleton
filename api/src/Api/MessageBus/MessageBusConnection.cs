using System;
using System.Threading.Tasks;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Utility;
using Foundatio.Skeleton.Domain.Services;
using Microsoft.AspNet.SignalR;
#pragma warning disable 4014

namespace Foundatio.Skeleton.Api.MessageBus {
    public class MessageBusConnection : PersistentConnection {
        private readonly IConnectionMapping _connectionMapping;

        public MessageBusConnection(IConnectionMapping connectionMapping) {
            _connectionMapping = connectionMapping;
        }

        protected override async Task OnConnected(IRequest request, string connectionId) {
            if (request.User.GetOrganizationId() != null)
                await _connectionMapping.GroupAddAsync(request.User.GetOrganizationId(), connectionId).AnyContext();

            await _connectionMapping.UserIdAddAsync(request.User.GetUserId(), connectionId).AnyContext();
        }

        protected override async Task OnDisconnected(IRequest request, string connectionId, bool stopCalled) {
            if (request.User.GetOrganizationId() != null)
                await _connectionMapping.GroupRemoveAsync(request.User.GetOrganizationId(), connectionId).AnyContext();

            await _connectionMapping.UserIdRemoveAsync(request.User.GetUserId(), connectionId).AnyContext();
        }

        protected override Task OnReconnected(IRequest request, string connectionId) {
            return OnConnected(request, connectionId);
        }

        protected override bool AuthorizeRequest(IRequest request) {
            return request.User.Identity.IsAuthenticated;
        }
    }
}
