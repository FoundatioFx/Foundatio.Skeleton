﻿using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Skeleton.Api.Extensions;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Services;

namespace Foundatio.Skeleton.Api.Security {
    public class AuthMessageHandler : DelegatingHandler {
        public const string BearerScheme = "bearer";
        public const string BasicScheme = "basic";
        public const string TokenScheme = "token";
        private static readonly Regex _authTokenRegex = new Regex("data-import/([a-zA-Z\\d]{24,40})", RegexOptions.Compiled);

        private readonly ITokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrganizationRepository _organizationRepository;

        public AuthMessageHandler(ITokenRepository tokenRepository, IUserRepository userRepository, IOrganizationRepository organizationRepository) {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _organizationRepository = organizationRepository;
        }

        protected virtual Task<HttpResponseMessage> BaseSendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            return base.SendAsync(request, cancellationToken);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var authHeader = request.Headers.Authorization;
            string scheme = authHeader?.Scheme.ToLower();
            string token = null;
            if (authHeader != null && (scheme == BearerScheme || scheme == TokenScheme))
                token = authHeader.Parameter;
            else if (authHeader != null && scheme == BasicScheme) {
                var authInfo = request.GetBasicAuth();
                if (authInfo != null) {
                    if (authInfo.Username.ToLower() == "client")
                        token = authInfo.Password;
                    else if (authInfo.Password.ToLower() == "x-oauth-basic" || String.IsNullOrEmpty(authInfo.Password))
                        token = authInfo.Username;
                    else {
                        User user;
                        try {
                            user = await _userRepository.GetByEmailAddressAsync(authInfo.Username);
                        } catch (Exception) {
                            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                        }

                        if (user == null || !user.IsActive)
                            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                        if (String.IsNullOrEmpty(user.Salt))
                            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                        string encodedPassword = authInfo.Password.ToSaltedHash(user.Salt);
                        if (!String.Equals(encodedPassword, user.Password))
                            return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                        await SetupUserRequest(request, user);

                        return await BaseSendAsync(request, cancellationToken);
                    }
                }
            } else {
                string queryToken = request.GetQueryString("access_token");
                if (!String.IsNullOrEmpty(queryToken))
                    token = queryToken;

                queryToken = request.GetQueryString("api_key");
                if (String.IsNullOrEmpty(token) && !String.IsNullOrEmpty(queryToken))
                    token = queryToken;

                queryToken = request.GetQueryString("apikey");
                if (String.IsNullOrEmpty(token) && !String.IsNullOrEmpty(queryToken))
                    token = queryToken;

                if (String.IsNullOrEmpty(token)) {
                    var match = _authTokenRegex.Match(request.RequestUri.AbsolutePath);
                    if (match.Success && match.Groups.Count == 2)
                        token = match.Groups[1].Value;
                }
            }

            if (String.IsNullOrEmpty(token))
                return await BaseSendAsync(request, cancellationToken);

            var tokenRecord = await _tokenRepository.GetByIdAsync(token, true);
            if (tokenRecord == null)
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);

            if (tokenRecord.ExpiresUtc.HasValue && tokenRecord.ExpiresUtc.Value < DateTime.UtcNow)
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);

            if (!String.IsNullOrEmpty(tokenRecord.UserId)) {
                var user = await _userRepository.GetByIdAsync(tokenRecord.UserId, true);
                if (user == null)
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                await SetupUserRequest(request, user, tokenRecord.OrganizationId);
            } else {
                await SetupTokenRequest(request, tokenRecord);
            }

            return await BaseSendAsync(request, cancellationToken);
        }

        private async Task SetupUserRequest(HttpRequestMessage request, User user, string organizationId = null) {
            request.GetRequestContext().Principal = new ClaimsPrincipal(user.ToIdentity(organizationId));
            request.SetUser(user);

            string selectedOrganizationId = organizationId ?? request.GetSelectedOrganizationId();
            var organization = await _organizationRepository.GetByIdAsync(selectedOrganizationId, true);
            if (organization != null)
                request.SetOrganization(organization);
        }

        private async Task SetupTokenRequest(HttpRequestMessage request, Token token) {
            request.GetRequestContext().Principal = new ClaimsPrincipal(token.ToIdentity());

            string selectedOrganizationId = token.OrganizationId ?? request.GetSelectedOrganizationId();
            var organization = await _organizationRepository.GetByIdAsync(selectedOrganizationId, true);
            if (organization != null)
                request.SetOrganization(organization);
        }
    }
}
