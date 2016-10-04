using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Skeleton.Api.Extensions;
using Exceptionless.DateTimeExtensions;
using Foundatio.Caching;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Api.Utility {
    public class ThrottlingHandler : DelegatingHandler {
        private readonly ICacheClient _cacheClient;
        private readonly Func<string, long> _maxRequestsForUserIdentifier;
        private readonly TimeSpan _period;
        private readonly string _message;

        public ThrottlingHandler(ICacheClient cacheClient, Func<string, long> maxRequestsForUserIdentifier, TimeSpan period, string message = "The allowed number of requests has been exceeded.") {
            _cacheClient = cacheClient;
            _maxRequestsForUserIdentifier = maxRequestsForUserIdentifier;
            _period = period;
            _message = message;
        }

        protected virtual string GetUserIdentifier(HttpRequestMessage request) {
            var authType = request.GetAuthType();
            if (authType == AuthType.Token)
                return request.GetSelectedOrganizationId();

            if (authType == AuthType.User) {
                var user = request.GetUser();
                if (user != null)
                    return user.Id;
            }

            // fallback to using the IP address
            var ip = request.GetClientIpAddress();
            if (String.IsNullOrEmpty(ip) || ip == "::1")
                ip = "127.0.0.1";

            return ip;
        }

        protected virtual string GetCacheKey(string userIdentifier) {
            return String.Concat("api", ":", userIdentifier, ":", DateTime.UtcNow.Floor(_period).Ticks);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            string identifier = GetUserIdentifier(request);
            if (String.IsNullOrEmpty(identifier))
                return await CreateResponse(request, HttpStatusCode.Forbidden, "Could not identify client.").AnyContext();

            long requestCount = 1;
            try {
                string cacheKey = GetCacheKey(identifier);
                requestCount = (long)await _cacheClient.IncrementAsync(cacheKey, 1, _period).AnyContext();
                if (requestCount == 1)
                    await _cacheClient.SetExpirationAsync(cacheKey, _period).AnyContext();
            } catch {}

            Task<HttpResponseMessage> response = null;
            long maxRequests = _maxRequestsForUserIdentifier(identifier);
            if (requestCount > maxRequests)
                response = CreateResponse(request, HttpStatusCode.Conflict, _message);
            else
                response = base.SendAsync(request, cancellationToken);

            HttpResponseMessage httpResponse = await response.AnyContext();

            long remaining = maxRequests - requestCount;
            if (remaining < 0)
                remaining = 0;

            httpResponse.Headers.Add(AppHeaders.RateLimit, maxRequests.ToString());
            httpResponse.Headers.Add(AppHeaders.RateLimitRemaining, remaining.ToString());

            return httpResponse;
        }

        protected Task<HttpResponseMessage> CreateResponse(HttpRequestMessage request, HttpStatusCode statusCode, string message) {
            var tsc = new TaskCompletionSource<HttpResponseMessage>();
            HttpResponseMessage response = request.CreateResponse(statusCode);
            response.ReasonPhrase = message;
            response.Content = new StringContent(message);
            tsc.SetResult(response);
            return tsc.Task;
        }
    }
}
