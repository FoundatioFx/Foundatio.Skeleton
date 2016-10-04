using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.Http;
using Foundatio.Caching;
using Foundatio.Jobs;
using Foundatio.Logging;
using Foundatio.Logging.Xunit;
using Foundatio.Queues;
using Foundatio.Storage;
using Nest;
using SimpleInjector;
using Xunit.Abstractions;

using Foundatio.Skeleton.Api;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Queues.Models;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Repositories.Configuration;
using Foundatio.Skeleton.Domain.Services;

namespace Foundatio.Skeleton.IntegrationTests {
    public abstract class TestBase : TestWithLoggingBase, IDisposable {
        private Container _container;
        private bool _initialized;

        public TestBase(ITestOutputHelper output) : base(output) { }

        public TService GetService<TService>() where TService : class {
            if (!_initialized)
                Initialize();

            return _container.GetInstance<TService>();
        }

        protected virtual void Initialize() {
            _container = GetDefaultContainer();
            _initialized = true;
        }

        protected virtual void RegisterServices(Container container) {
            var bootstrapper = new Bootstrapper();
            bootstrapper.RegisterServices(container);
        }

        public Container GetDefaultContainer() {
            var container = AppBuilder.CreateContainer(Log, _logger, false);
            RegisterServices(container);
            container.RunStartupActionsAsync().GetAwaiter().GetResult();
            return container;
        }

        public Container GetEmptyContainer() {
            return new Container();
        }

        public virtual void Dispose() {
            _container?.Dispose();
        }
    }

    public abstract class IntegrationTestsBase : TestBase {
        private readonly IUserRepository _userRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly SampleDataService _sampleDataService;
        private User _testUser;
        private Organization _testOrganization;
        private readonly AppElasticConfiguration _configuration;

        protected IntegrationTestsBase(ITestOutputHelper output) : base(output) {
            _configuration = GetService<AppElasticConfiguration>();

            ResetAllAsync().GetAwaiter().GetResult();

            _userRepository = GetService<IUserRepository>();
            _organizationRepository = GetService<IOrganizationRepository>();
            _sampleDataService = GetService<SampleDataService>();
        }

        protected async Task<HttpRequestMessage> CreateRequestAsync(Foundatio.Skeleton.Domain.Models.Token token, string currentOrganizationId = null, HttpMethod method = null, string url = "api/v1/contacts", bool isCompressed = false, string mediaType = "application/json", string charSet = "utf-8") {
            var context = new OwinContext();

            if (token != null) {
                // Unpack claims principle, organization via token
                var principal = new ClaimsPrincipal(token.ToIdentity());

                if (String.IsNullOrEmpty(currentOrganizationId))
                    currentOrganizationId = principal.GetOrganizationId();

                context.Request.User = principal;
                return await CreateRequestAsync(context, currentOrganizationId, method, url, isCompressed, mediaType, charSet);
            }

            return null;
        }

        protected async Task<HttpRequestMessage> CreateRequestAsync(User user, string currentOrganizationId = null, HttpMethod method = null, string url = "api/v1/contacts", bool isCompressed = false, string mediaType = "application/json", string charSet = "utf-8", IPrincipal principal = null) {
            var context = new OwinContext();

            if (user != null) {
                if (String.IsNullOrEmpty(currentOrganizationId))
                    currentOrganizationId = user.Memberships.Any() ? user.Memberships.First().OrganizationId : null;

                context.Request.User = new ClaimsPrincipal(IdentityService.CreateUserIdentity(user.EmailAddress, user.Id, new List<string>(), new Membership {
                    OrganizationId = currentOrganizationId,
                    //Roles = new List<string> { AuthorizationRoles.GlobalAdmin }
                }));

                context.Set("User", user);
            }

            context.Set("Organization", _testOrganization != null && _testOrganization.Id == currentOrganizationId ? _testOrganization : await _organizationRepository.GetByIdAsync(currentOrganizationId, true));
            context.Request.User = new ClaimsPrincipal(IdentityService.CreateUserIdentity(user.EmailAddress, user.Id, new List<string>(), new Membership {
                OrganizationId = currentOrganizationId,
                //Roles = new List<string> { AuthorizationRoles.GlobalAdmin }
            }));

            return await CreateRequestAsync(context, currentOrganizationId, method, url, isCompressed, mediaType, charSet);
        }

        private Task<HttpRequestMessage> CreateRequestAsync(OwinContext context, string currentOrganizationId = null, HttpMethod method = null, string url = "api/v1/contacts", bool isCompressed = false, string mediaType = "application/json", string charSet = "utf-8") {
            if (method == null)
                method = HttpMethod.Get;

            var request = new HttpRequestMessage();
            request.SetOwinContext(context);
            var configuration = new HttpConfiguration();

            AppBuilder.SetupRoutes(configuration);
            configuration.EnsureInitialized();
            request.Method = method;
            request.RequestUri = new Uri($"http://localhost/{url}");

            request.SetConfiguration(configuration);
            request.Content = new HttpMessageContent(new HttpRequestMessage(method, url));
            if (isCompressed)
                request.Content.Headers.ContentEncoding.Add("gzip");
            request.Content.Headers.ContentType.MediaType = mediaType;
            request.Content.Headers.ContentType.CharSet = charSet;

            return Task.FromResult(request);
        }

        protected Task<HttpRequestMessage> CreatePostAsync(User user, string currentOrganizationId = null, bool isCompressed = false) {
            return CreateRequestAsync(user, currentOrganizationId, HttpMethod.Post, null, isCompressed);
        }

        protected Task<HttpRequestMessage> CreateAnonymousPostAsync(User user, string currentOrganizationId = null, bool isCompressed = false, string mediaType = "application/json", string charSet = "utf-8") {
            return CreateRequestAsync(user, currentOrganizationId, HttpMethod.Post, null, isCompressed, mediaType, charSet);
        }

        protected async Task<HttpRequestMessage> CreateTestUserPostAsync(bool isCompressed = false, string mediaType = "application/json", string charSet = "utf-8") {
            return await CreateRequestAsync(await GetTestUserAsync(), null, HttpMethod.Post, null, isCompressed, mediaType, charSet);
        }

        protected Task<HttpRequestMessage> CreateAnonymousRequestAsync(User user, string currentOrganizationId = null) {
            return CreateRequestAsync(user, currentOrganizationId, HttpMethod.Get);
        }

        protected async Task<HttpRequestMessage> CreateTestUserRequestAsync() {
            return await CreateRequestAsync(await GetTestUserAsync(), null, HttpMethod.Get);
        }

        protected async Task AddTestDataAsync() {
            var oldLoggingLevel = Log.MinimumLevel;
            Log.MinimumLevel = LogLevel.Warning;

            await _sampleDataService.CreateTestDataAsync();

            Log.MinimumLevel = oldLoggingLevel;
        }

        protected Task SaveOrganizationAsync(Organization org) {
            return _organizationRepository.SaveAsync(org);
        }

        protected Task SaveUserAsync(User user) {
            return _userRepository.SaveAsync(user);
        }

        protected async Task ResetAllAsync() {
            var oldLoggingLevel = Log.MinimumLevel;
            Log.MinimumLevel = LogLevel.Warning;

            await _configuration.DeleteIndexesAsync();

            var cacheClient = GetService<ICacheClient>();
            await cacheClient.RemoveAllAsync().AnyContext();

            var fileStorage = GetService<IFileStorage>();
            await fileStorage.DeleteFilesAsync(await fileStorage.GetFileListAsync().AnyContext());

            await GetService<IQueue<MailMessage>>().DeleteQueueAsync().AnyContext();
            await GetService<IQueue<WorkItemData>>().DeleteQueueAsync().AnyContext();

            Log.MinimumLevel = oldLoggingLevel;
            await _configuration.ConfigureIndexesAsync().AnyContext();
        }

        private IList<string> GetIndexList(IElasticClient client) {
            var result = client.CatIndices(
                d => d.RequestConfiguration(r =>
                    r.RequestTimeout(5 * 60 * 1000)));

            return result.Records.Select(i => i.Index).ToList();
        }

        private IList<CatAliasesRecord> GetAliasList(IElasticClient client) {
            var result = client.CatAliases(
                d => d.RequestConfiguration(r =>
                    r.RequestTimeout(5 * 60 * 1000)));

            return result.Records.ToList();
        }

        protected void RefreshData() {
            var esClient = GetService<IElasticClient>();
            esClient.Refresh();
        }

        protected async Task<User> GetTestUserAsync() {
            if (_testUser == null) {
                await AddTestDataAsync();
                RefreshData();
                _testUser = await _userRepository.GetByEmailAddressAsync(SampleDataService.TEST_USER_EMAIL);
            }

            return _testUser;
        }

        protected async Task<Organization> GetTestOrganizationAsync(string orgId = SampleDataService.TEST_ORG_ID) {
            if (_testOrganization == null) {
                await AddTestDataAsync();
                RefreshData();
                _testOrganization = await _organizationRepository.GetByIdAsync(orgId);
            }

            return _testOrganization;
        }
    }
}
