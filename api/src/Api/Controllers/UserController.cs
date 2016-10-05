using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

using AutoMapper;
using Exceptionless;
using Foundatio.Logging;
using Foundatio.Messaging;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Api.Models.Auth;
using Foundatio.Skeleton.Api.Security;
using Foundatio.Skeleton.Api.Utility;
using Swashbuckle.Swagger.Annotations;

using Foundatio.Skeleton.Api.Extensions;
using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.JsonPatch;
using Foundatio.Skeleton.Core.Utility;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Models.Messaging;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Services;

namespace Foundatio.Skeleton.Api.Controllers {
    [RoutePrefix(API_PREFIX + "/users")]
    [Authorize(Roles = AuthorizationRoles.User)]
    public class UserController : RepositoryApiController<IUserRepository, User, ViewUser, User, UpdateUser> {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly OrganizationService _organizationService;
        private readonly ITemplatedMailService _mailer;
        private readonly ITokenRepository _tokenRepository;
        private readonly IPublicFileStorage _publicFileStorage;
        private readonly IMessagePublisher _messagePublisher;

        public UserController(
            ILoggerFactory loggerFactory,
            IUserRepository userRepository,
            IOrganizationRepository organizationRepository,
            IPublicFileStorage publicFileStorage,
            OrganizationService organizationService,
            ITemplatedMailService mailer,
            ITokenRepository tokenRepository,
            IMapper mapper,
            IMessagePublisher messagePublisher) : base(loggerFactory, userRepository, mapper) {

            _organizationRepository = organizationRepository;
            _organizationService = organizationService;
            _publicFileStorage = publicFileStorage;
            _mailer = mailer;
            _tokenRepository = tokenRepository;
            _messagePublisher = messagePublisher;
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewCurrentUser))]
        [HttpGet]
        [Route("me")]
        public async Task<IHttpActionResult> GetCurrentUser() {
            var currentUser = await GetModel(CurrentUser.Id);
            if (currentUser == null)
                return NotFound();

            var viewUser = new ViewCurrentUser(currentUser, GetUserRoles(), GetSelectedOrganizationId());
            var orgs = (await _organizationRepository.GetByIdsAsync(viewUser.Memberships.Select(m => m.OrganizationId).ToArray(), useCache: true));
            foreach (var m in viewUser.Memberships) {
                var org = orgs.FirstOrDefault(o => o.Id == m.OrganizationId);
                if (org != null)
                    m.OrganizationName = org.Name;
            }

            return Ok(viewUser);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewUser))]
        [HttpGet]
        [Route("{id:objectid}", Name = "GetUserById")]
        public override Task<IHttpActionResult> GetById(string id) {
            return base.GetById(id);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ICollection<ViewUser>))]
        [HttpGet]
        [Route]
        [RequireOrganization]
        public override Task<IHttpActionResult> Get(string f = null, string q = null, string sort = null, string offset = null, string mode = null, int page = 1, int limit = 10, string facet = null) {
            return base.Get(f, q, sort, offset, mode, page, limit, facet);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewUser))]
        [HttpPatch]
        [Route("{id:objectid}")]
        public override Task<IHttpActionResult> PatchAsync(string id, PatchDocument changes, long? version = null) {
            return base.PatchAsync(id, changes, version);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewUser))]
        [HttpPut]
        [Route("{id:objectid}")]
        public override Task<IHttpActionResult> PutAsync(string id, ViewUser user, long? version = null) {
            return base.PutAsync(id, user, version);
        }

        [HttpDelete]
        [Route("{id:objectid}")]
        [OverrideAuthorization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        public override Task<IHttpActionResult> DeleteAsync(string id) {
            return base.DeleteAsync(id);
        }

        [HttpDelete]
        [Route("{id:objectid}/remove")]
        [OverrideAuthorization]
        [RequireOrganization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        public async Task<IHttpActionResult> RemoveAsync(string id) {
            var user = await GetModel(id, false);
            if (user == null)
                return NotFound();

            var organizationId = GetSelectedOrganizationId();
            var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId.Equals(organizationId));
            if (membership != null) {
                ////  todo:  need to revisit whether to hard delete the user if this is the only remaining membership
                //if (user.Memberships.Count == 1)
                //    return await base.DeleteAsync(id);

                user.Memberships.Remove(membership);
                await _repository.SaveAsync(user, true);

                await _messagePublisher.PublishAsync(new UserMembershipChanged {
                    ChangeType = ChangeType.Removed,
                    UserId = user.Id,
                    OrganizationId = organizationId
                }, TimeSpan.FromSeconds(1.5)).AnyContext();
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [Route("{id:objectid}/email-address/{email:minlength(1)}")]
        [Authorize(Roles = AuthorizationRoles.User)]
        [RequireOrganization]
        public async Task<IHttpActionResult> UpdateEmailAddress(string id, string email) {
            var user = await GetModel(id, false);
            if (user == null)
                return NotFound();

            if (String.Equals(CurrentUser.EmailAddress, email, StringComparison.OrdinalIgnoreCase))
                return Ok(new { IsVerified = user.IsEmailAddressVerified });

            email = email.ToLower();
            if (!await IsEmailAddressAvailableInternal(email))
                return BadRequest("A user with this email address already exists.");

            user.EmailAddress = email;

            await UpdateModelAsync(user);

            if (!user.IsEmailAddressVerified)
                await ResendVerificationEmail(id);

            return Ok(new { IsVerified = user.IsEmailAddressVerified });
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("verify-email-address")]
        public async Task<IHttpActionResult> Verify(string token) {
            var user = await _repository.GetByVerifyEmailAddressTokenAsync(token);
            if (user == null)
                return NotFound();

            if (!user.HasValidEmailAddressTokenExpiration())
                return BadRequest("Verify Email Address Token has expired.");

            user.MarkEmailAddressVerified();

            await _repository.SaveAsync(user);

            var admins = user.GetMembershipsWithAdminRole();
            if (admins != null && admins.Any())
                foreach (var membership in user.Memberships)
                    await _organizationService.TryMarkOrganizationAsVerifiedAsync(membership.OrganizationId);

            ExceptionlessClient.Default.CreateFeatureUsage("Verify Email Address").AddObject(user).Submit();

            if (user.Password == null) {
                // TODO(derek): when we get last org in there, use that
                var t = await _tokenRepository.GetOrCreateUserToken(user.Id, null);
                return Ok(new TokenResponseModel { Token = t.Id });
            }

            return Ok();
        }

        [HttpGet]
        [Route("{id:objectid}/resend-verification-email")]
        public async Task<IHttpActionResult> ResendVerificationEmail(string id) {
            var user = await GetModel(id, false);
            if (user == null)
                return NotFound();

            if (!user.IsEmailAddressVerified) {
                user.CreateVerifyEmailAddressToken();
                await _repository.SaveAsync(user);
                _mailer.SendVerifyEmail(user);
            }

            return Ok();
        }

        [HttpPost]
        [Route("{id:objectid}/admin-role")]
        [OverrideAuthorization]
        [RequireOrganization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        public async Task<IHttpActionResult> AddAdminRole(string id) {
            var user = await GetModel(id, false);
            if (user == null)
                return NotFound();

            var organizationId = GetSelectedOrganizationId();
            if (user.AddedAdminMembershipRoles(organizationId)) {
                await _repository.SaveAsync(user, true);

                await _messagePublisher.PublishAsync(new UserMembershipChanged {
                    ChangeType = ChangeType.Added,
                    UserId = user.Id,
                    OrganizationId = organizationId
                }, TimeSpan.FromSeconds(1.5)).AnyContext();
            }

            return Ok();
        }

        [HttpDelete]
        [Route("{id:objectid}/admin-role")]
        [OverrideAuthorization]
        [RequireOrganization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        public async Task<IHttpActionResult> DeleteAdminRole(string id) {
            var user = await GetModel(id, false);
            if (user == null)
                return NotFound();

            var organizationId = GetSelectedOrganizationId();
            var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId.Equals(organizationId));
            if (membership != null) {
                if (membership.Roles.Contains(AuthorizationRoles.Admin)) {
                    membership.Roles.Remove(AuthorizationRoles.Admin);
                    await _repository.SaveAsync(user, true);

                    await _messagePublisher.PublishAsync(new UserMembershipChanged {
                        ChangeType = ChangeType.Removed,
                        UserId = user.Id,
                        OrganizationId = organizationId
                    }, TimeSpan.FromSeconds(1.5)).AnyContext();
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [Route("{id:objectid}/global-admin-role")]
        [OverrideAuthorization]
        [Authorize(Roles = AuthorizationRoles.GlobalAdmin)]
        public async Task<IHttpActionResult> AddGlobalAdminRole(string id) {
            var user = await GetModel(id, false);
            if (user == null)
                return NotFound();

            if (user.AddedGlobalAdminRole()) {
                await _repository.SaveAsync(user, true);

                await _messagePublisher.PublishAsync(new UserMembershipChanged {
                    ChangeType = ChangeType.Added,
                    UserId = user.Id
                }, TimeSpan.FromSeconds(1.5)).AnyContext();
            }

            return Ok();
        }

        [HttpDelete]
        [Route("{id:objectid}/global-admin-role")]
        [OverrideAuthorization]
        [Authorize(Roles = AuthorizationRoles.GlobalAdmin)]
        public async Task<IHttpActionResult> DeleteGlobalAdminRole(string id) {
            var user = await GetModel(id, false);
            if (user == null)
                return NotFound();

            if (user.Roles.Contains(AuthorizationRoles.GlobalAdmin)) {
                user.Roles.Remove(AuthorizationRoles.GlobalAdmin);
                await _repository.SaveAsync(user, true);

                await _messagePublisher.PublishAsync(new UserMembershipChanged {
                    ChangeType = ChangeType.Removed,
                    UserId = user.Id
                }, TimeSpan.FromSeconds(1.5)).AnyContext();
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [Route("{id:objectid}/data/{key:minlength(1)}")]
        public async Task<IHttpActionResult> SetDataValue(string id, string key, [NakedBody]string value) {
            if (String.IsNullOrWhiteSpace(value))
                return BadRequest();

            var user = await GetModel(id);
            if (user == null)
                return NotFound();

            if (user.Data == null)
                user.Data = new DataDictionary();

            user.Data[key] = value;
            await _repository.SaveAsync(user);

            return Ok();
        }

        [HttpDelete]
        [Route("{id:objectid}/data/{key:minlength(1)}")]
        public async Task<IHttpActionResult> DeleteDataValue(string id, string key) {
            var user = await GetModel(id);
            if (user == null)
                return NotFound();

            if (user.Data == null || !user.Data.ContainsKey(key))
                return NotFound();

            user.Data.Remove(key);
            await _repository.SaveAsync(user);

            return Ok();
        }

        private async Task<bool> IsEmailAddressAvailableInternal(string email) {
            if (String.IsNullOrWhiteSpace(email))
                return false;

            if (CurrentUser != null && String.Equals(CurrentUser.EmailAddress, email, StringComparison.OrdinalIgnoreCase))
                return true;

            return await _repository.GetByEmailAddressAsync(email) == null;
        }

        protected override async Task<User> GetModel(string id, bool useCache = true) {
            if (Request.IsAdmin() || String.Equals(CurrentUser.Id, id))
                return await base.GetModel(id, useCache);

            return null;
        }

        protected override Task<IReadOnlyCollection<User>> GetModels(string[] ids, bool useCache = true) {
            if (Request.IsAdmin())
                return base.GetModels(ids, useCache);

            return base.GetModels(ids.Where(id => String.Equals(CurrentUser.Id, id)).ToArray(), useCache);
        }
    }
}
