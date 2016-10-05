using System;
using System.Threading.Tasks;
using System.Web.Http;

using AutoMapper;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Api.Security;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;

namespace Foundatio.Skeleton.Api.Controllers {
    [RoutePrefix(API_PREFIX + "/log")]
    [Authorize(Roles = AuthorizationRoles.User)]
    [RequireOrganization]
    public class LogController : ReadOnlyRepositoryApiController<ILogRepository, LogEvent, LogEvent> {
        public LogController(ILogRepository repository, IMapper mapper) : base(repository, mapper) { }

        [HttpGet]
        [RequireOrganization]
        [Route]
        public async Task<IHttpActionResult> Get(DateTime? start = null, DateTime? end = null, string f = null, string q = null, int page = 1, int limit = 50) {
            var orgId = User.IsInRole(AuthorizationRoles.GlobalAdmin) ? null : GetSelectedOrganizationId();
            var results = await _repository.GetEntriesAsync(orgId, start, end, f, q, new PagingOptions { Page = page, Limit = limit });

            return OkWithResourceLinks(results.Documents, results.HasMore && !NextPageExceedsSkipLimit(page, limit), page, results.Total);
        }
    }
}
