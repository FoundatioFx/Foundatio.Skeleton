using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Results;

using Xunit;
using Xunit.Abstractions;

using Foundatio.Skeleton.Api.Controllers;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;

namespace Foundatio.Skeleton.IntegrationTests.API {
    public class NotificationControllerTests : IntegrationTestsBase {
        public NotificationControllerTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task NotificationController_GetUnreadCount_is_correct_when_no_readers() {
            // setup
            var notificationRepo = GetService<INotificationRepository>();
            var org = await GetTestOrganizationAsync();

            await notificationRepo.AddAsync(new Notification {
                OrganizationId = org.Id,
                Message = "hey",
                Readers = new HashSet<string>(),
            });

            await notificationRepo.AddAsync(new Notification {
                OrganizationId = org.Id,
                Message = "hey2",
                Readers = new HashSet<string>(),
            });

            RefreshData();

            // act
            var controller = GetService<NotificationController>();
            controller.Request = await CreateTestUserRequestAsync();

            var result = await controller.GetUnreadCount() as OkNegotiatedContentResult<UnreadNotificationsCount>;

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Content.Unread);
        }

        [Fact]
        public async Task NotificationController_GetUnreadCount_is_correct_after_reading() {
            // setup
            var notificationRepo = GetService<INotificationRepository>();
            var org = await GetTestOrganizationAsync();

            await notificationRepo.AddAsync(new Notification {
                OrganizationId = org.Id,
                Message = "hey",
                Readers = new HashSet<string>(),
            });

            var toRead = await notificationRepo.AddAsync(new Notification {
                OrganizationId = org.Id,
                Message = "hey2",
                Readers = new HashSet<string>(),
            });

            RefreshData();

            // act
            var controller = GetService<NotificationController>();
            controller.Request = await CreateTestUserRequestAsync();

            await controller.MarkRead(new[] { toRead.Id });

            RefreshData();

            var result = await controller.GetUnreadCount() as OkNegotiatedContentResult<UnreadNotificationsCount>;

            // assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Content.Unread);
        }
    }
}
