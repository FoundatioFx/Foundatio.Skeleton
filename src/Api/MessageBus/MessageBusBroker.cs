using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Logging;
using Foundatio.Messaging;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Utility;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Models.Messaging;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Foundatio.Skeleton.Api.MessageBus {
    public sealed class MessageBusBroker {
        private readonly IConnectionManager _connectionManager;
        private readonly IConnectionMapping _connectionMapping;
        private readonly IMessageSubscriber _subscriber;
        private readonly ILogger _logger;

        public MessageBusBroker(IConnectionManager connectionManager, IConnectionMapping connectionMapping, IMessageSubscriber subscriber, ILogger<MessageBusBroker> logger) {
            _connectionManager = connectionManager;
            _connectionMapping = connectionMapping;
            _subscriber = subscriber;
            _logger = logger;
        }

        public void Start() {
            _subscriber.Subscribe<EntityChanged>(OnEntityChangedAsync);
            _subscriber.Subscribe<UserMembershipChanged>(OnUserMembershipChangedAsync);
        }

        private async Task OnUserMembershipChangedAsync(UserMembershipChanged userMembershipChanged, CancellationToken cancellationToken = default(CancellationToken)) {
            if (String.IsNullOrEmpty(userMembershipChanged?.OrganizationId))
                return;

            // manage user organization group membership
            foreach (var connectionId in await _connectionMapping.GetUserIdConnectionsAsync(userMembershipChanged.UserId)) {
                if (userMembershipChanged.ChangeType == ChangeType.Added)
                    await _connectionMapping.GroupAddAsync(userMembershipChanged.OrganizationId, connectionId).AnyContext();
                else if (userMembershipChanged.ChangeType == ChangeType.Removed)
                    await _connectionMapping.GroupRemoveAsync(userMembershipChanged.OrganizationId, connectionId);
            }

            await GroupSendAsync(userMembershipChanged.OrganizationId, userMembershipChanged);
        }

        private async Task OnEntityChangedAsync(EntityChanged entityChanged, CancellationToken cancellationToken = default(CancellationToken)) {
            if (entityChanged == null)
                return;

            var appEntityChanged = AppEntityChanged.Create(entityChanged);
            if (appEntityChanged.Type == typeof(User).Name) {
                foreach (var connectionId in await _connectionMapping.GetConnectionsAsync(appEntityChanged.Id))
                    await Context.Connection.TypedSendAsync(connectionId, appEntityChanged);

                return;
            }

            if (!String.IsNullOrEmpty(appEntityChanged.OrganizationId))
                await GroupSendAsync(appEntityChanged.OrganizationId, appEntityChanged);

            if (appEntityChanged.Type == "Organization")
                await GroupSendAsync(appEntityChanged.Id, appEntityChanged);
        }

        private async Task GroupSendAsync(string group, object value) {
            var connectionIds = await _connectionMapping.GetGroupConnectionsAsync(group).AnyContext();
            await Context.Connection.TypedSendAsync(connectionIds.ToList(), value);
        }

        private IPersistentConnectionContext Context => _connectionManager.GetConnectionContext<MessageBusConnection>();
    }

    public static class MessageBrokerExtensions {
        public static Task TypedSendAsync(this IConnection connection, string connectionId, object value) {
            return connection.Send(connectionId, new TypedMessage { Type = GetMessageType(value), Message = value });
        }

        public static Task TypedSendAsync(this IConnection connection, IList<string> connectionIds, object value) {
            return connection.Send(connectionIds, new TypedMessage { Type = GetMessageType(value), Message = value });
        }

        public static Task TypedBroadcastAsync(this IConnection connection, object value) {
            return connection.Broadcast(new TypedMessage { Type = GetMessageType(value), Message = value });
        }

        private static string GetMessageType(object value) {
            return value.GetType().Name;
        }
    }

    public class TypedMessage {
        public string Type { get; set; }
        public object Message { get; set; }
    }
}
