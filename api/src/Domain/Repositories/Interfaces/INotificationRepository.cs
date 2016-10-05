using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Domain.Models;

namespace Foundatio.Skeleton.Domain.Repositories {
    public interface INotificationRepository : IRepositoryOwnedByOrganization<Notification> {
        Task<FindResults<Notification>> GetAccessibleAsync(string organizationId, string userId, string query, PagingOptions page);
        Task<long> GetUnreadCountAsync(string organizationId, string userId);
        Task MarkReadAsync(ICollection<string> ids, string userId);
    }
}
