using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Foundatio.Skeleton.Api.Controllers;
using Foundatio.Skeleton.Api.Serialization;

namespace Foundatio.Skeleton.Api.Utility.Results {
    public class PermissionActionResult : IHttpActionResult {
        public PermissionActionResult(PermissionResult permission, HttpRequestMessage request) {
            if (permission == null)
                throw new ArgumentNullException("permission");

            if (request == null)
                throw new ArgumentNullException("request");

            Permission = permission;
            Request = request;
        }

        public PermissionResult Permission { get; private set; }

        public HttpRequestMessage Request { get; private set; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken) {
            return Task.FromResult(Execute());
        }

        private HttpResponseMessage Execute() {
            return new HttpResponseMessage(Permission.StatusCode) {
                ReasonPhrase = Permission.Message,
                Content = new ObjectContent<MessageContent>(new MessageContent(Permission.Id, Permission.Message), new AppJsonMediaTypeFormatter()),
                RequestMessage = Request
            };
        }
    }
}