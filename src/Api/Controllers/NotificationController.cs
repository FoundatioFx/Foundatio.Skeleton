using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

using AutoMapper;
using Foundatio.Logging;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Api.Security;
using Foundatio.Skeleton.Api.Extensions;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;

namespace Foundatio.Skeleton.Api.Controllers {
    [RoutePrefix(API_PREFIX + "/notification")]
    [Authorize(Roles = AuthorizationRoles.User)]
    [RequireOrganization]
    public class NotificationController : RepositoryApiController<INotificationRepository, Notification, ViewNotification, NewNotification, Notification> {
        public NotificationController(ILoggerFactory loggerFactory, INotificationRepository repository, IMapper mapper)
            : base(loggerFactory, repository, mapper) {
        }

        [HttpGet]
        [Route]
        public async Task<IHttpActionResult> Get(string q = null, int page = 1, int limit = 10) {
            var organizationId = GetSelectedOrganizationId();
            var userId = Request.GetUser()?.Id;

            if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(userId))
                return NotFound();

            page = GetPage(page);
            limit = GetLimit(limit);
            var options = new PagingOptions { Page = page, Limit = limit };

            var findResults = await _repository.GetAccessibleAsync(organizationId, userId, q, options);

            var list = new List<ViewNotification>();
            foreach (var doc in findResults.Documents)
                list.Add(await Map<ViewNotification>(doc, true));

            return OkWithResourceLinks(list, findResults.HasMore, page, findResults.Total);
        }

        [HttpGet]
        [Route("{id:objectid}", Name = "GetNotificationById")]
        public override Task<IHttpActionResult> GetById(string id) {
            return base.GetById(id);
        }

        [HttpGet]
        [Route("unread", Name = "GetUnreadCount")]
        public async Task<IHttpActionResult> GetUnreadCount() {
            var organizationId = GetSelectedOrganizationId();
            var userId = CurrentUser?.Id;
            if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(userId))
                return NotFound();

            var count = await _repository.GetUnreadCountAsync(organizationId, userId);

            return Ok(new UnreadNotificationsCount { Unread = count });
        }

        [HttpPost]
        [Route("markread", Name = "MarkRead")]
        public async Task<IHttpActionResult> MarkRead(string[] ids) {
            var userId = Request.GetUser()?.Id;
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            await _repository.MarkReadAsync(ids, userId);

            return Ok();
        }

        [HttpPost]
        [Route("purge", Name = "Purge")]
        public Task<IHttpActionResult> Purge(string[] ids) {
            return base.DeleteAsync(ids);
        }

        [HttpPost]
        [Route]
        public override Task<IHttpActionResult> PostAsync(NewNotification value) {
            return base.PostAsync(value);
        }

        [HttpDelete]
        [Route("{ids:objectids}", Name = "DeleteNotificationByIds")]
        public override Task<IHttpActionResult> DeleteAsync(string[] ids) {
            return base.DeleteAsync(ids);
        }

        protected override async Task<Notification> GetModel(string id, bool useCache = true) {
            if (String.IsNullOrEmpty(id))
                return null;

            var model = await _repository.GetByIdAsync(id, useCache);
            if (model == null)
                return null;

            if (model.OrganizationId != GetSelectedOrganizationId())
                return null;

            if (!String.IsNullOrEmpty(model.UserId) && model.UserId != Request.GetUser()?.Id)
                return null;

            return model;
        }
    }
}
