using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

using AutoMapper;
using Foundatio.Logging;
using Foundatio.Repositories;
using Foundatio.Repositories.Models;

using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.JsonPatch;
using Foundatio.Skeleton.Core.Serialization;
using Foundatio.Skeleton.Domain.Models;
using Newtonsoft.Json.Linq;

namespace Foundatio.Skeleton.Api.Controllers {
    public abstract class RepositoryApiController<TRepository, TModel, TViewModel, TNewModel, TUpdateModel> : ReadOnlyRepositoryApiController<TRepository, TModel, TViewModel>
            where TRepository : ISearchableRepository<TModel>
            where TModel : class, IIdentity, new()
            where TViewModel : class, IIdentity, new()
            where TUpdateModel : class, new()
            where TNewModel : class, new() {

        protected readonly ILogger _logger;

        protected RepositoryApiController(ILoggerFactory loggerFactory, TRepository repository, IMapper mapper) : base(repository, mapper) {
            _logger = loggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;
        }

        #region Add

        public virtual async Task<IHttpActionResult> PostAsync(TNewModel value) {
            if (value == null)
                return BadRequest();

            TModel mapped = await Map<TModel>(value);

            var orgModel = mapped as IOwnedByOrganization;
            // if no organization id is specified, default to the selected org.
            if (!_isOrganization && orgModel != null)
                orgModel.OrganizationId = GetSelectedOrganizationId();

            var permission = await CanAddAsync(mapped);
            if (!permission.Allowed)
                return Permission(permission);

            TModel model = await AddModelAsync(mapped);
            if (model == null)
                return BadRequest("Failed to add model");

            model = await AfterAddAsync(model);

            return Created(new Uri(GetEntityLink(model.Id)), await Map<TViewModel>(model, true));
        }

        protected virtual Task<PermissionResult> CanAddAsync(TModel value) {
            var orgModel = value as IOwnedByOrganization;
            if (_isOrganization || orgModel == null)
                return Task.FromResult(PermissionResult.Allow);

            if (!CanAccessOrganization(orgModel.OrganizationId))
                return Task.FromResult(PermissionResult.DenyWithMessage("Invalid organization id specified."));

            return Task.FromResult(PermissionResult.Allow);
        }

        protected virtual Task<TModel> AddModelAsync(TModel value) {
            return _repository.AddAsync(value);
        }

        protected virtual Task<TModel> AfterAddAsync(TModel value) {
            return Task.FromResult(value);
        }

        protected virtual string GetEntityLink(string id) {
            return Url.Link($"Get{typeof(TModel).Name}ById", new { id });
        }

        protected virtual string GetEntityResourceLink(string id, string type) {
            return GetResourceLink(Url.Link($"Get{typeof(TModel).Name}ById", new { id }), type);
        }

        protected virtual string GetEntityLink<TEntityType>(string id) {
            return Url.Link($"Get{typeof(TEntityType).Name}ById", new { id });
        }

        protected virtual string GetEntityResourceLink<TEntityType>(string id, string type) {
            return GetResourceLink(Url.Link($"Get{typeof(TEntityType).Name}ById", new { id }), type);
        }

        #endregion

        #region Update

        public virtual async Task<IHttpActionResult> PatchAsync(string id, PatchDocument changes, long? version = null) {
            TModel original = await GetModel(id, false);
            if (original == null)
                return NotFound();

            // if there are no changes in the delta, then ignore the request
            if (changes == null || !changes.Operations.Any())
                return await OkModel(original);

            var modified = original.Copy();
            if (version.HasValue && _isVersioned)
                ((IVersioned)modified).Version = version.Value;

            // map to update model first so we can restrict what is allowed to be modified
            var updateModel = await Map<TUpdateModel>(modified);

            // Supports either JsonPatch or JSONPath syntax: http://goessner.net/articles/JsonPath/
            var target = JToken.FromObject(updateModel, JsonHelper.DefaultSerializer);
            new JsonPatcher().Patch(ref target, changes);
            updateModel = target.ToObject<TUpdateModel>(JsonHelper.DefaultSerializer);

            await Map(updateModel, modified);

            var permission = await CanUpdateAsync(original, modified);
            if (!permission.Allowed)
                return Permission(permission);

            modified = await UpdateModelAsync(modified, version);
            modified = await AfterUpdateAsync(modified, original);

            return await OkModel(modified);
        }

        public virtual async Task<IHttpActionResult> PutAsync(string id, TViewModel model, long? version = null) {
            TModel original = await GetModel(id, false);
            if (original == null)
                return NotFound();

            var modified = original.Copy();
            // map to update model first so we can restrict what is allowed to be modified
            var updateModel = await Map<TUpdateModel>(modified);
            updateModel = await Map(model, updateModel);
            modified = await Map(updateModel, modified);
            if (version.HasValue && _isVersioned)
                ((IVersioned)modified).Version = version.Value;

            var permission = await CanUpdateAsync(original, modified);
            if (!permission.Allowed)
                return Permission(permission);

            modified = await UpdateModelAsync(modified, version);
            modified = await AfterUpdateAsync(modified, original);

            return await OkModel(modified);
        }

        protected virtual async Task<PermissionResult> CanUpdateAsync(TModel original, TModel modified) {
            if (original.Id != modified.Id)
                return PermissionResult.DenyWithMessage("Id must match resource.");

            var orgModel = original as IOwnedByOrganization;
            var modifiedOrgModel = modified as IOwnedByOrganization;
            if (orgModel != null && !CanAccessOrganization(orgModel.OrganizationId))
                return PermissionResult.DenyWithMessage("Invalid organization id specified.");

            if (orgModel?.OrganizationId != modifiedOrgModel?.OrganizationId)
                return PermissionResult.DenyWithMessage("Invalid organization id specified.");

            return PermissionResult.Allow;
        }

        protected virtual Task<TModel> UpdateModelAsync(TModel modified, long? version = null) {
            return _repository.SaveAsync(modified);
        }

        protected virtual Task<TModel> AfterUpdateAsync(TModel value, TModel original) {
            return Task.FromResult(value);
        }

        protected async Task<IHttpActionResult> UpdateModelsAsync(string[] ids, Func<UpdateModelContext<TModel>, Task> modelUpdateFunc) {
            var results = await UpdateModelsInternalAsync(ids, modelUpdateFunc);

            if (typeof(TViewModel) == typeof(TModel))
                return Ok(results);

            return Ok(await Map<ICollection<TViewModel>>(results, true));
        }

        protected async Task<IHttpActionResult> UpdateModelAsync(string id, Func<UpdateModelContext<TModel>, Task> modelUpdateFunc) {
            var result = await UpdateModelsInternalAsync(new[] { id }, modelUpdateFunc);
            if (result.HttpResult != null)
                return result.HttpResult;

            if (typeof(TViewModel) == typeof(TModel))
                return Ok(result.Models.FirstOrDefault());

            return Ok(await Map<TViewModel>(result.Models.FirstOrDefault(), true));
        }

        private async Task<UpdateModelsResult<TModel>> UpdateModelsInternalAsync(string[] ids, Func<UpdateModelContext<TModel>, Task> modelUpdateFunc) {
            var originals = await GetModels(ids);
            if (originals == null || originals.Count == 0)
                return new UpdateModelsResult<TModel> { HttpResult = NotFound() };

            var modifiedModels = new List<TModel>();
            foreach (var original in originals) {
                var modifiedModel = original.Copy();
                modifiedModels.Add(modifiedModel);
                var ctx = new UpdateModelContext<TModel>(modifiedModel);
                await modelUpdateFunc(ctx);
                if (ctx.HttpResult != null)
                    return new UpdateModelsResult<TModel> { HttpResult = ctx.HttpResult };
            }

            await _repository.SaveAsync(modifiedModels);

            foreach (var original in originals)
                await AfterUpdateAsync(modifiedModels.FirstOrDefault(m => m.Id == original.Id), original);

            return new UpdateModelsResult<TModel> { Models = modifiedModels };
        }

        #endregion

        #region Delete

        public virtual async Task<IHttpActionResult> UndeleteAsync(string id) {
            if (!_supportsSoftDeletes)
                return StatusCode(HttpStatusCode.BadRequest);

            var model = await GetModel(id, false);
            if (model == null)
                return NotFound();

            var permission = await CanDeleteAsync(model);
            if (!permission.Allowed)
                return Permission(permission);

            try {
                await UndeleteModelAsync(model);
                await AfterUndeleteAsync(model);
            } catch (Exception ex) {
                _logger.Error(ex, String.Empty);
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            return await OkModel(model);
        }

        public virtual async Task<IHttpActionResult> UndeleteAsync(string[] ids) {
            if (!_supportsSoftDeletes)
                return StatusCode(HttpStatusCode.BadRequest);

            var items = (await GetModels(ids, false)).ToList();
            if (!items.Any())
                return NotFound();

            var results = new ModelActionResults();
            results.AddNotFound(ids.Except(items.Select(i => i.Id)));

            foreach (var model in items.ToList()) {
                var permission = await CanDeleteAsync(model);
                if (permission.Allowed)
                    continue;

                items.Remove(model);
                results.Failure.Add(permission);
            }

            if (items.Count == 0)
                return results.Failure.Count == 1 ? Permission(results.Failure.First()) : BadRequest(results);

            try {
                await UndeleteModelsAsync(items);
                foreach (var item in items)
                    await AfterUndeleteAsync(item);
            } catch (Exception ex) {
                _logger.Error().Exception(ex).Write();
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            if (results.Failure.Count == 0)
                return StatusCode(HttpStatusCode.NoContent);

            results.Success.AddRange(items.Select(i => i.Id));
            return BadRequest(results);
        }

        public virtual async Task<IHttpActionResult> DeleteAsync(string id) {
            var model = await GetModel(id, false);
            if (model == null)
                return NotFound();

            var permission = await CanDeleteAsync(model);
            if (!permission.Allowed)
                return Permission(permission);

            try {
                await DeleteModelAsync(model);
                await AfterDeleteAsync(model);
            } catch (Exception ex) {
                _logger.Error().Exception(ex).Write();
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            return await OkModel(model);
        }

        public virtual async Task<IHttpActionResult> DeleteAsync(string[] ids) {
            var items = (await GetModels(ids, false)).ToList();
            if (!items.Any())
                return NotFound();

            var results = new ModelActionResults();
            results.AddNotFound(ids.Except(items.Select(i => i.Id)));

            foreach (var model in items.ToList()) {
                var permission = await CanDeleteAsync(model);
                if (permission.Allowed)
                    continue;

                items.Remove(model);
                results.Failure.Add(permission);
            }

            if (items.Count == 0)
                return results.Failure.Count == 1 ? Permission(results.Failure.First()) : BadRequest(results);

            try {
                await DeleteModelsAsync(items);
                foreach (var item in items)
                    await AfterDeleteAsync(item);
            } catch (Exception ex) {
                _logger.Error().Exception(ex).Write();
                return StatusCode(HttpStatusCode.InternalServerError);
            }

            if (results.Failure.Count == 0)
                return StatusCode(HttpStatusCode.NoContent);

            results.Success.AddRange(items.Select(i => i.Id));
            return BadRequest(results);
        }

        protected virtual async Task<PermissionResult> CanDeleteAsync(TModel value) {
            var orgModel = value as IOwnedByOrganization;
            if (orgModel != null && !CanAccessOrganization(orgModel.OrganizationId))
                return PermissionResult.DenyWithNotFound(value.Id);

            return PermissionResult.Allow;
        }

        protected virtual async Task DeleteModelAsync(TModel value) {
            if (_supportsSoftDeletes) {
                ((ISupportSoftDeletes)value).IsDeleted = true;
                await _repository.SaveAsync(value);
            } else
                await _repository.RemoveAsync(value);
        }

        protected virtual async Task DeleteModelsAsync(ICollection<TModel> values) {
            if (_supportsSoftDeletes) {
                values.Cast<ISupportSoftDeletes>().ForEach(v => v.IsDeleted = true);
                await _repository.SaveAsync(values);
            } else
                await _repository.RemoveAsync(values);
        }

        protected virtual async Task UndeleteModelAsync(TModel value) {
            if (!_supportsSoftDeletes)
                return;

            ((ISupportSoftDeletes)value).IsDeleted = false;
            await _repository.SaveAsync(value);
        }

        protected virtual async Task UndeleteModelsAsync(ICollection<TModel> values) {
            if (!_supportsSoftDeletes)
                return;

            values.Cast<ISupportSoftDeletes>().ForEach(v => v.IsDeleted = false);
            await _repository.SaveAsync(values);
        }

        protected virtual Task<TModel> AfterDeleteAsync(TModel value) {
            return Task.FromResult(value);
        }

        protected virtual Task<TModel> AfterUndeleteAsync(TModel value) {
            return Task.FromResult(value);
        }

        #endregion
    }

    public class UpdateModelsResult<T> {
        public ICollection<T> Models { get; set; } = new List<T>();
        public IHttpActionResult HttpResult { get; set; }
    }

    public class UpdateModelContext<T> {
        public UpdateModelContext(T model) {
            Model = model;
        }

        public T Model { get; set; }
        public IHttpActionResult HttpResult { get; set; }
    }
}
