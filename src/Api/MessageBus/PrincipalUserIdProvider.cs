using System;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Services;
using Microsoft.AspNet.SignalR;

namespace Foundatio.Skeleton.Api.MessageBus {
    public class PrincipalUserIdProvider : IUserIdProvider {
        public string GetUserId(IRequest request) {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.User?.Identity != null && request.User.GetAuthType() == AuthType.User)
                return request.User.GetUserId();

            return null;
        }
    }
}
