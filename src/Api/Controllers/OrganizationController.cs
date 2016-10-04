using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Foundatio.Logging;
using Foundatio.Messaging;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Api.Security;
using Swashbuckle.Swagger.Annotations;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.JsonPatch;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Models.Messaging;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Repositories.Query;
using Foundatio.Skeleton.Domain.Services;

namespace Foundatio.Skeleton.Api.Controllers {
    [RoutePrefix(API_PREFIX + "/organizations")]
    [Authorize(Roles = AuthorizationRoles.User)]
    public class OrganizationController : RepositoryApiController<IOrganizationRepository, Organization, ViewOrganization, NewOrganization, NewOrganization> {
        private readonly IUserRepository _userRepository;
        private readonly ITemplatedMailService _mailer;
        private readonly IMessagePublisher _messagePublisher;

        public OrganizationController(IOrganizationRepository organizationRepository,
            ILoggerFactory loggerFactory,
            IUserRepository userRepository,
            ITemplatedMailService mailer,
            IMessagePublisher messagePublisher,
            IMapper mapper)
            : base(loggerFactory, organizationRepository, mapper) {
            _userRepository = userRepository;
            _mailer = mailer;
            _messagePublisher = messagePublisher;
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ICollection<ViewOrganization>))]
        [HttpGet]
        [Route]
        [RequireOrganization]
        public async Task<IHttpActionResult> Get() {
            var organizationIds = CurrentUser.Memberships.Select(m => m.OrganizationId).ToArray();
            var organizations = new List<ViewOrganization>();
            foreach (var org in (await _repository.GetByIdsAsync(organizationIds)))
                organizations.Add(await Map<ViewOrganization>(org, true));

            return Ok(organizations);
        }

        [HttpGet]
        [OverrideAuthorization]
        [Authorize(Roles = AuthorizationRoles.GlobalAdmin)]
        [Route("admin")]
        public Task<IHttpActionResult> GetForAdmins(string f = null, string q = null, string sort = null, string offset = null, string mode = null, int page = 1, int limit = 10) {
            return GetInternal(new SystemFilterQuery(), f, q, sort, offset, mode, page, limit);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewOrganization))]
        [HttpGet]
        [Route("{id:objectid}", Name = "GetOrganizationById")]
        public override async Task<IHttpActionResult> GetById(string id) {
            var organization = await GetModel(id);
            if (organization == null)
                return NotFound();

            var viewOrganization = await Map<ViewOrganization>(organization, true);
            return Ok(viewOrganization);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewOrganization))]
        [HttpPost]
        [Route]
        public override Task<IHttpActionResult> PostAsync(NewOrganization value) {
            return base.PostAsync(value);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewOrganization))]
        [HttpPatch]
        [Route("{id:objectid}")]
        public override Task<IHttpActionResult> PatchAsync(string id, [FromBody]PatchDocument changes, long? version = null) {
            return base.PatchAsync(id, changes, version);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(ViewOrganization))]
        [HttpPut]
        [Route("{id:objectid}")]
        public override Task<IHttpActionResult> PutAsync(string id, ViewOrganization organization, long? version = null) {
            return base.PutAsync(id, organization, version);
        }

        [HttpDelete]
        [Route("{id:objectid}")]
        public override Task<IHttpActionResult> DeleteAsync(string id) {
            return base.DeleteAsync(id);
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IHttpActionResult> Delete([FromBody]EntitySelection selection) {
            if (selection == null || ((selection.Ids == null || selection.Ids.Length == 0) && selection.Filter == null))
                return StatusCode(HttpStatusCode.BadRequest);

            if (selection.Ids != null && selection.Ids.Length > 0)
                return await base.DeleteAsync(selection.Ids);

            if (selection.Filter != null) {
                //  queue work item here
            }

            return StatusCode(HttpStatusCode.BadRequest);
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("invites/{token:minlength(1)}")]
        public async Task<IHttpActionResult> GetInvites(string token) {
            if (string.IsNullOrEmpty(token))
                return NotFound();

            var result = await _repository.GetByInviteTokenAsync(token).AnyContext();
            if (result == null)
                return NotFound();

            return Ok(result.Item2);
        }

        [HttpGet]
        [RequireOrganization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        [Route("invites")]
        public async Task<IHttpActionResult> GetInvites() {
            string organizationId = GetSelectedOrganizationId();
            Organization organization = await GetModel(organizationId);
            if (organization == null)
                return BadRequest();


            var invites = organization.Invites.Select(i => {
                var fullName = i.EmailAddress;
                if (!String.IsNullOrWhiteSpace(i.FullName))
                    fullName = i.FullName;

                return new ViewUser {
                    EmailAddress = i.EmailAddress,
                    FullName = fullName,
                    Memberships = new List<Membership> {
                        new Membership { OrganizationId = organizationId, Roles = i.Roles }
                    }
                };

            }).ToList();

            return Ok(invites);
        }

        [HttpPost]
        [RequireOrganization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        [Route("invites")]
        public async Task<IHttpActionResult> AddInvite(InviteUser model) {
            if (String.IsNullOrEmpty(model.EmailAddress))
                throw new ValidationException(new[] { new ValidationFailure("email_address", "Please enter an email address") });

            var role = model.Role;

            if (String.IsNullOrEmpty(role))
                role = AuthorizationRoles.User;

            if (!AuthorizationRoles.AllScopes.Contains(role))
                return BadRequest();

            Organization organization = await GetModel(GetSelectedOrganizationId());
            if (organization == null)
                return BadRequest();

            var currentUser = CurrentUser;

            //  need to check for global admin here
            var addGlobalAdmin = false;
            if (role == AuthorizationRoles.GlobalAdmin) {
                if (currentUser.IsGlobalAdmin()) {
                    addGlobalAdmin = true;
                    role = AuthorizationRoles.Admin;
                } else {
                    // downgrade to current user's role
                    role = currentUser.IsAdmin(organization.Id) ? AuthorizationRoles.Admin : AuthorizationRoles.User;
                }
            } else if (role == AuthorizationRoles.Admin && !currentUser.IsAdmin(organization.Id)) {
                role = AuthorizationRoles.User;
            }

            var user = await _userRepository.GetByEmailAddressAsync(model.EmailAddress);

            // user exists, just add them to the org
            if (user != null) {
                if (addGlobalAdmin) {
                    //  will be false only if user is already a global admin
                    addGlobalAdmin = user.AddedGlobalAdminRole();
                }

                if (user.AddedMembershipRole(organization.Id, role) || addGlobalAdmin) {
                    await _userRepository.SaveAsync(user);
                    await _messagePublisher.PublishAsync(new UserMembershipChanged {
                        ChangeType = ChangeType.Added,
                        UserId = user.Id,
                        OrganizationId = organization.Id
                    }, TimeSpan.FromSeconds(1.5)).AnyContext();
                }

                _mailer.SendAddedToOrganization(currentUser, organization, user);

                return Ok(new InviteUserResponse { Added = true, UserId = user.Id, EmailAddress = model.EmailAddress });
            }

            Invite invite = organization.Invites.FirstOrDefault(i => String.Equals(i.EmailAddress, model.EmailAddress, StringComparison.OrdinalIgnoreCase));
            if (invite == null) {
                invite = new Invite {
                    Token = StringUtils.GetNewToken(),
                    EmailAddress = model.EmailAddress.ToLower(),
                    FullName = model.FullName,
                    DateAdded = DateTime.UtcNow,
                    AddedByUserId = currentUser.Id,
                    Roles = addGlobalAdmin
                    ? AuthorizationRoles.GetScope(AuthorizationRoles.GlobalAdmin)
                    : AuthorizationRoles.GetScope(role)
                };
                organization.Invites.Add(invite);
                await _repository.SaveAsync(organization);
            }

            _mailer.SendInvite(currentUser, organization, invite);

            return Ok(new InviteUserResponse { Invited = true, EmailAddress = model.EmailAddress });
        }

        [HttpGet]
        [RequireOrganization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        [Route("invites/{email:minlength(1)}/resend-invite")]
        public async Task<IHttpActionResult> ResendInvite(string email) {
            var organization = await GetModel(GetSelectedOrganizationId());
            if (organization == null)
                return BadRequest();

            var currentUser = CurrentUser;

            var invite = organization.Invites.FirstOrDefault(i => String.Equals(i.EmailAddress, email, StringComparison.OrdinalIgnoreCase));
            if (invite == null)
                return NotFound();

            _mailer.SendInvite(currentUser, organization, invite);
            return Ok();
        }

        [HttpDelete]
        [RequireOrganization]
        [Authorize(Roles = AuthorizationRoles.Admin)]
        [Route("invites/{email:minlength(1)}")]
        public async Task<IHttpActionResult> RemoveInvite(string email) {
            if (String.IsNullOrEmpty(email))
                return BadRequest();

            Organization organization = await GetModel(GetSelectedOrganizationId());
            if (organization == null)
                return BadRequest();

            var invite = organization.Invites.FirstOrDefault(i => String.Equals(i.EmailAddress, email, StringComparison.OrdinalIgnoreCase));
            if (invite == null)
                return Ok();

            organization.Invites.Remove(invite);

            await UpdateModelAsync(organization);

            return Ok();
        }

        protected override async Task<Organization> AddModelAsync(Organization value) {
            var organization = await base.AddModelAsync(value);

            // add current user to the new org
            CurrentUser.AddAdminMembership(organization.Id);

            await _userRepository.SaveAsync(CurrentUser, true);

            await _messagePublisher.PublishAsync(new UserMembershipChanged {
                UserId = CurrentUser.Id,
                OrganizationId = organization.Id,
                ChangeType = ChangeType.Added
            }, TimeSpan.FromSeconds(1.5));

            return organization;
        }

        protected override async Task DeleteModelsAsync(ICollection<Organization> organizations) {
            var currentUser = CurrentUser;

            foreach (var organization in organizations) {
                _logger.Info("User {0} deleting organization {1}.", currentUser.Id, organization.Id);

                var users = (await _userRepository.GetByOrganizationIdAsync(organization.Id)).Documents;
                foreach (User user in users) {
                    // delete the user if they are not associated to any other organizations and they are not the current user
                    if (user.Memberships.All(m => String.Equals(m.OrganizationId, organization.Id)) && !String.Equals(user.Id, currentUser.Id)) {
                        _logger.Info("Removing user '{0}' as they do not belong to any other organizations.", user.Id, organization.Name, organization.Id);
                        await _userRepository.RemoveAsync(user.Id);
                    } else {
                        _logger.Info("Removing user '{0}' from organization '{1}' with Id: '{2}'", user.Id, organization.Name, organization.Id);
                        var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId == organization.Id);
                        user.Memberships.Remove(membership);
                        await _userRepository.SaveAsync(user);
                    }
                }

                _logger.Info("Deleting organization '{0}' with Id: '{1}'.", organization.Name, organization.Id);
                await base.DeleteModelsAsync(new[] { organization });
            }
        }
    }
}
