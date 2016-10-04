using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Results;
using Exceptionless.DateTimeExtensions;
using Foundatio.Skeleton.Api.Controllers.Base;
using Foundatio.Skeleton.Api.Security;
using Foundatio.Skeleton.Api.Utility.Results;
using Foundatio.Skeleton.Api.Extensions;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Services;
using Foundatio.Skeleton.Domain.Repositories.Query;

namespace Foundatio.Skeleton.Api.Controllers {
    [RequireHttpsExceptLocal]
    public abstract class AppApiController : ApiController {
        public const string API_PREFIX = "api/v1";
        protected const int DEFAULT_LIMIT = 10;
        protected const int MAXIMUM_LIMIT = 1000;
        protected const int MAXIMUM_SKIP = 2000;

        public AppApiController() {
            AllowedTimeRangeFields = new List<string>();
        }

        protected TimeSpan GetOffset(string offset) {
            double offsetInMinutes;
            if (!String.IsNullOrEmpty(offset) && Double.TryParse(offset, out offsetInMinutes))
                return TimeSpan.FromMinutes(offsetInMinutes);

            return TimeSpan.Zero;
        }

        protected ICollection<string> AllowedTimeRangeFields { get; private set; }

        protected virtual TimeInfo GetTimeInfo(string time, string offset) {
            string field = null;
            if (!String.IsNullOrEmpty(time) && time.Contains("|")) {
                var parts = time.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                field = parts.Length > 0 && AllowedTimeRangeFields.Contains(parts[0]) ? parts[0] : null;
                time = parts.Length > 1 ? parts[1] : null;
            }

            var utcOffset = GetOffset(offset);

            // range parsing needs to be based on the user's local time.
            var localRange = DateTimeRange.Parse(time, DateTime.UtcNow.Add(utcOffset));
            var utcRange = localRange != DateTimeRange.Empty ? localRange.Subtract(utcOffset) : localRange;

            return new TimeInfo {
                Field = field,
                Offset = utcOffset,
                UtcRange = utcRange
            };
        }

        protected int GetLimit(int limit) {
            if (limit < 1)
                limit = DEFAULT_LIMIT;
            else if (limit > MAXIMUM_LIMIT)
                limit = MAXIMUM_LIMIT;

            return limit;
        }

        protected int GetPage(int page) {
            if (page < 1)
                page = 1;

            return page;
        }

        protected int GetSkip(int currentPage, int limit) {
            if (currentPage < 1)
                currentPage = 1;

            int skip = (currentPage - 1) * limit;
            if (skip < 0)
                skip = 0;

            return skip;
        }

        public User CurrentUser => Request.GetUser();

        public Organization Organization => Request.GetOrganization();

        public AuthType AuthType => User.GetAuthType();

        public virtual string GetSelectedOrganizationId() {
            return Organization != null ? Organization.Id : Request.GetSelectedOrganizationId();
        }

        public string[] GetUserRoles() {
            return Request.GetUserRoles();
        }

        public bool CanAccessOrganization(string organizationId) {
            return Request.CanAccessOrganization(organizationId);
        }

        protected StatusCodeActionResult StatusCodeWithMessage(HttpStatusCode statusCode, string message) {
            return new StatusCodeActionResult(statusCode, Request, message);
        }

        protected IHttpActionResult BadRequest(ModelActionResults results) {
            return new NegotiatedContentResult<ModelActionResults>(HttpStatusCode.BadRequest, results, this);
        }

        public PermissionActionResult Permission(PermissionResult permission) {
            return new PermissionActionResult(permission, Request);
        }

        public NotImplementedActionResult NotImplemented(string message) {
            return new NotImplementedActionResult(message, Request);
        }

        public OkContentDownloadResult<T> OkContentDownload<T>(T content, string fileName) {
            return new OkContentDownloadResult<T>(content, this, fileName);
        }

        public OkWithHeadersContentResult<T> OkWithLinks<T>(T content, params string[] links) {
            return new OkWithHeadersContentResult<T>(content, this, links.Where(l => l != null).Select(l => new KeyValuePair<string, IEnumerable<string>>("Link", new[] { l })));
        }

        public OkWithHeadersContentResult<T> OkWithHeaders<T>(T content, params Tuple<string, string>[] headers) {
            return new OkWithHeadersContentResult<T>(content, this, headers.Where(h => h != null).Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Item1, new[] { h.Item2 })));
        }

        public OkWithHeadersContentResult<T> OkWithHeaders<T>(T content, params Tuple<string, string[]>[] headers) {
            return new OkWithHeadersContentResult<T>(content, this, headers.Select(h => new KeyValuePair<string, IEnumerable<string>>(h.Item1, h.Item2)));
        }

        public OkWithHeadersContentResult<T> OkWithHeaders<T>(T content, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers) {
            return new OkWithHeadersContentResult<T>(content, this, headers);
        }

        public OkWithResourceLinks<IReadOnlyCollection<TEntity>, TEntity> OkWithResourceLinks<TEntity>(IReadOnlyCollection<TEntity> content, bool hasMore, Func<TEntity, string> pagePropertyAccessor = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null, bool isDescending = false) where TEntity : class {
            return new OkWithResourceLinks<IReadOnlyCollection<TEntity>, TEntity>(content, this, hasMore, null, pagePropertyAccessor, null, headers, isDescending);
        }

        public OkWithResourceLinks<ResultWithFacets<TEntity>, TEntity> OkWithResourceLinks<TEntity>(ResultWithFacets<TEntity> content, bool hasMore, int page, long total, Func<TEntity, string> pagePropertyAccessor = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null, bool isDescending = false) where TEntity : class {
            return new OkWithResourceLinks<ResultWithFacets<TEntity>, TEntity>(content, this, hasMore, page, total, pagePropertyAccessor, c => c.Results, headers, isDescending);
        }

        public OkWithResourceLinks<IReadOnlyCollection<TEntity>, TEntity> OkWithResourceLinks<TEntity>(IReadOnlyCollection<TEntity> content, bool hasMore, int page, long total, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null) where TEntity : class {
            return new OkWithResourceLinks<IReadOnlyCollection<TEntity>, TEntity>(content, this, hasMore, page, total);
        }

        protected string GetResourceLink(string url, string type) {
            return url != null ? $"<{url}>; rel=\"{type}\"" : null;
        }

        protected bool NextPageExceedsSkipLimit(int page, int limit) {
            return (page + 1) * limit >= MAXIMUM_SKIP;
        }

        public SystemFilterQuery GetSystemFilter(bool hasOrganizationFilter, bool supportsSoftDeletes) {
            var result = new SystemFilterQuery();
            if (supportsSoftDeletes)
                result.IncludeSoftDeletes = false;

            if (hasOrganizationFilter && Request.IsGlobalAdmin())
                return result;

            result.OrganizationIds.Add(Request.GetSelectedOrganizationId());
            return result;
        }

        protected bool HasOrganizationFilter(string filter) {
            if (String.IsNullOrWhiteSpace(filter))
                return false;

            return filter.Contains("organization:");
        }
    }
}
