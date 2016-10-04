using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

using AutoMapper;
using Foundatio.Logging;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Api.Security;
using Foundatio.Skeleton.Api.Extensions;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;

namespace Foundatio.Skeleton.Api.Controllers {
    [RoutePrefix(API_PREFIX + "/tokens")]
    [Authorize(Roles = AuthorizationRoles.User)]
    [RequireOrganization]
    public class TokenController : RepositoryApiController<ITokenRepository, Token, ViewToken, NewToken, Token> {
        public TokenController(ILoggerFactory loggerFactory, ITokenRepository repository, IMapper mapper)
            : base(loggerFactory, repository, mapper) {
        }

        [HttpGet]
        public async Task<IHttpActionResult> Get(int page = 1, int limit = 10) {
            var organizationId = GetSelectedOrganizationId();
            if (String.IsNullOrEmpty(organizationId))
                return NotFound();

            page = GetPage(page);
            limit = GetLimit(limit);
            var tokenResults = await _repository.GetApiTokensAsync(organizationId, new PagingOptions { Page = page, Limit = limit });
            var tokens = new List<ViewToken>();
            foreach (var token in tokenResults.Documents)
                tokens.Add(await Map<ViewToken>(token, true));

            return OkWithResourceLinks(tokens, tokenResults.HasMore, page, tokenResults.Total);
        }

        [HttpGet]
        [Route("{id:token}", Name = "GetTokenById")]
        public override Task<IHttpActionResult> GetById(string id) {
            return base.GetById(id);
        }

        [Route]
        [HttpPost]
        public override Task<IHttpActionResult> PostAsync(NewToken value) {
            return base.PostAsync(value);
        }

        protected override async Task<Token> GetModel(string id, bool useCache = true) {
            if (String.IsNullOrEmpty(id))
                return null;

            var model = await _repository.GetByIdAsync(id, useCache);
            if (model == null)
                return null;

            if (!String.IsNullOrEmpty(model.OrganizationId) && model.OrganizationId == GetSelectedOrganizationId())
                return null;

            if (!String.IsNullOrEmpty(model.UserId) && model.UserId != Request.GetUser().Id)
                return null;

            if (model.Type != TokenType.Access)
                return null;

            return model;
        }

        protected override async Task<PermissionResult> CanAddAsync(Token value) {
            if (String.IsNullOrEmpty(value.OrganizationId))
                return PermissionResult.Deny;

            foreach (string scope in value.Scopes.ToList()) {
                if (scope != scope.ToLower()) {
                    value.Scopes.Remove(scope);
                    value.Scopes.Add(scope.ToLower());
                }

                if (!AuthorizationRoles.AllScopes.Contains(scope.ToLower()))
                    return PermissionResult.DenyWithMessage("Invalid token scope requested.");
            }

            if (value.Scopes.Count == 0)
                value.Scopes.Add(AuthorizationRoles.Client);

            if (value.Scopes.Contains(AuthorizationRoles.Client) && !User.IsInRole(AuthorizationRoles.User))
                return PermissionResult.Deny;

            if (value.Scopes.Contains(AuthorizationRoles.User) && !User.IsInRole(AuthorizationRoles.User))
                return PermissionResult.Deny;

            if (value.Scopes.Contains(AuthorizationRoles.GlobalAdmin) && !User.IsInRole(AuthorizationRoles.GlobalAdmin))
                return PermissionResult.Deny;

            return await base.CanAddAsync(value);
        }

        protected override Task<Token> AddModelAsync(Token value) {
            value.Id = StringUtils.GetNewToken();
            value.CreatedUtc = value.UpdatedUtc = DateTime.UtcNow;
            value.Type = TokenType.Access;
            value.CreatedBy = Request.GetUser().Id;

            // add implied scopes
            if (value.Scopes.Contains(AuthorizationRoles.GlobalAdmin))
                value.Scopes.Add(AuthorizationRoles.User);

            if (value.Scopes.Contains(AuthorizationRoles.User))
                value.Scopes.Add(AuthorizationRoles.Client);

            return base.AddModelAsync(value);
        }
    }
}
