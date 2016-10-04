using System.Collections.Generic;
using System.Threading.Tasks;
using Foundatio.Repositories.Elasticsearch.Queries.Builders;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Core.Extensions;
using Nest;
using Foundatio.Repositories.Queries;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories.Configuration;
using SortOrder = Foundatio.Repositories.Models.SortOrder;

namespace Foundatio.Skeleton.Domain.Repositories {
    public class NotificationRepository : RepositoryOwnedByOrganization<Notification>, INotificationRepository {
        public NotificationRepository(AppElasticConfiguration configuration)
            : base(configuration.Organizations.NotificationType) {
        }

        public Task<FindResults<Notification>> GetAccessibleAsync(string organizationId, string userId, string query, PagingOptions page) {
            var filter = Filter<Notification>.Term(e => e.UserId, userId)
                || Filter<Notification>.Missing(e => e.UserId);

            var options = new CrmQuery()
                .WithOrganizationId(organizationId)
                .WithElasticFilter(filter)
                .WithSearchQuery(query, false)
                .WithSort("created", SortOrder.Descending)
                .WithPaging(page);

            return FindAsync(options);
        }

        public async Task<long> GetUnreadCountAsync(string organizationId, string userId) {
            var filter = (Filter<Notification>.Term(e => e.UserId, userId) || Filter<Notification>.Missing(e => e.UserId))
                && Filter<Notification>.Not(f => f.Term(e => e.Readers, userId));

            var options = new CrmQuery()
                .WithOrganizationId(organizationId)
                .WithElasticFilter(filter);

            var result = await CountAsync(options).AnyContext();

            return result.Total;
        }

        public async Task MarkReadAsync(ICollection<string> ids, string userId) {
            var notifications = await GetByIdsAsync(ids).AnyContext();
            foreach (var notification in notifications)
                notification.Readers.Add(userId);

            await SaveAsync(notifications).AnyContext();
        }
    }
}
