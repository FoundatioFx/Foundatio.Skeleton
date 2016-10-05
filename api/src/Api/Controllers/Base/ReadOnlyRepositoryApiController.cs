using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

using AutoMapper;
using Exceptionless;
using Foundatio.Repositories;
using Foundatio.Repositories.Models;
using Foundatio.Repositories.Queries;
using Foundatio.Skeleton.Api.Controllers.Base;
using Foundatio.Skeleton.Core.Models;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories.Query;

namespace Foundatio.Skeleton.Api.Controllers {
    public abstract class ReadOnlyRepositoryApiController<TRepository, TModel, TViewModel> : AppApiController
            where TRepository : ISearchableReadOnlyRepository<TModel>
            where TModel : class, new()
            where TViewModel : class, new() {
        protected readonly TRepository _repository;
        protected static readonly bool _isOwnedByOrganization = typeof(IOwnedByOrganization).IsAssignableFrom(typeof(TModel));
        protected static readonly bool _isOrganization = typeof(TModel) == typeof(Organization);
        protected static readonly bool _supportsSoftDeletes = typeof(ISupportSoftDeletes).IsAssignableFrom(typeof(TModel));
        protected static readonly bool _isVersioned = typeof(IVersioned).IsAssignableFrom(typeof(TModel));
        protected readonly IMapper _mapper;

        public ReadOnlyRepositoryApiController(TRepository repository, IMapper mapper) {
            _repository = repository;
            _mapper = mapper;
        }

        #region Get

        public virtual async Task<IHttpActionResult> GetById(string id) {
            TModel model = await GetModel(id);
            if (model == null)
                return NotFound();

            return await OkModel(model);
        }

        protected async Task<IHttpActionResult> OkModel(TModel model) {
            if (typeof(TViewModel) == typeof(TModel) && _mapper.ConfigurationProvider.FindTypeMapFor<TModel, TViewModel>() == null)
                return Ok(model);

            return Ok(await Map<TViewModel>(model, true));
        }

        protected virtual async Task<TModel> GetModel(string id, bool useCache = true) {
            if (String.IsNullOrEmpty(id))
                return null;

            TModel model = await _repository.GetByIdAsync(id, useCache);
            if (_isOwnedByOrganization && model != null && ((IOwnedByOrganization)model).OrganizationId != GetSelectedOrganizationId())
                return null;

            return model;
        }

        protected virtual async Task<IReadOnlyCollection<TModel>> GetModels(string[] ids, bool useCache = true) {
            if (ids == null || ids.Length == 0)
                return new List<TModel>();

            IReadOnlyCollection<TModel> models = await _repository.GetByIdsAsync(ids, useCache: useCache);
            var selectedOrganizationId = GetSelectedOrganizationId();
            if (_isOwnedByOrganization)
                models = models?.Where(m => ((IOwnedByOrganization)m).OrganizationId == selectedOrganizationId).ToList();

            return models;
        }

        public virtual Task<IHttpActionResult> Get(string userFilter = null, string query = null, string sort = null, string offset = null, string mode = null, int page = 1, int limit = 10, string facet = null) {
            return GetInternal(null, userFilter, query, sort, offset, mode, page, limit, facet);
        }

        public virtual async Task<IHttpActionResult> GetFacets(string facets, string userFilter = null, string query = null) {
            var systemFilter = GetSystemFilter(HasOrganizationFilter(query), _supportsSoftDeletes);

            var fo = AggregationOptions.Parse(facets);
            var res = await _repository.GetAggregationsAsync(systemFilter, fo, userFilter);

            return Ok(res);
        }

        public virtual async Task<IHttpActionResult> GetCount(string userFilter = null, string query = null, string facet = null) {
            var systemFilter = GetSystemFilter(HasOrganizationFilter(query), _supportsSoftDeletes);

            var fo = AggregationOptions.Parse(facet);
            var res = await _repository.CountBySearchAsync(systemFilter, userFilter, fo);

            return Ok(res);
        }

        public async Task<IHttpActionResult> GetInternal(SystemFilterQuery systemFilter = null, string userFilter = null, string query = null, string sort = null, string offset = null, string mode = null, int page = 1, int limit = 10, string facet = null) {
            page = GetPage(page);
            limit = GetLimit(limit);
            var skip = GetSkip(page + 1, limit);
            if (skip > MAXIMUM_SKIP)
                return BadRequest("Cannot get requested page");

            if (systemFilter == null)
                systemFilter = GetSystemFilter(HasOrganizationFilter(query), _supportsSoftDeletes);

            var fo = AggregationOptions.Parse(facet);

            FindResults<TModel> results;
            try {
                results = await _repository.SearchAsync(systemFilter, userFilter, query, sort, new PagingOptions { Limit = limit, Page = page }, fo);
            } catch (ApplicationException ex) {
                ex.ToExceptionless().SetProperty("Search Filter", new { SystemFilter = systemFilter, UserFilter = query, Sort = sort, Offset = offset, Page = page, Limit = limit }).AddTags("Search").Submit();
                return BadRequest("An error has occurred. Please check your search filter.");
            }

            if (!String.IsNullOrEmpty(mode) && String.Equals(mode, "summary", StringComparison.InvariantCultureIgnoreCase))
                return OkWithResourceLinks(await MapCollection<TViewModel>(results.Documents, true), results.HasMore, page, results.Total);

            var mappedModels = await MapCollection<TViewModel>(results.Documents, true);
            var hasMore = results.HasMore && !NextPageExceedsSkipLimit(page, limit);

            if (facet != null) {
                var wrappedResult = new ResultWithFacets<TViewModel> { Results = mappedModels, Facets = results.Aggregations };
                return OkWithResourceLinks(wrappedResult, hasMore, page, results.Total);
            }

            return OkWithResourceLinks(mappedModels, hasMore, page, results.Total);
        }

        #endregion

        #region Mapping

        protected async Task<TDestination> Map<TDestination>(object source, bool isResult = false) {
            var destination = _mapper.Map<TDestination>(source);

            if (isResult)
                await AfterResultMap(new List<TDestination>(new[] { destination }));
            return destination;
        }

        protected async Task<TDestination> Map<TDestination>(object source, TDestination destination, bool isResult = false) {
            destination = _mapper.Map(source, destination);

            if (isResult)
                await AfterResultMap(new List<TDestination>(new[] { destination }));
            return destination;
        }

        protected async Task<IReadOnlyCollection<TDestination>> MapCollection<TDestination>(object source, bool isResult = false) {
            var destination = _mapper.Map<IReadOnlyCollection<TDestination>>(source);

            if (isResult)
                await AfterResultMap<TDestination>(destination);
            return destination;
        }

        protected virtual async Task AfterResultMap<TDestination>(IReadOnlyCollection<TDestination> models) {
            foreach (var model in models.OfType<IHaveData>())
                model.Data.RemoveSensitiveData();
        }

        #endregion
    }
}
