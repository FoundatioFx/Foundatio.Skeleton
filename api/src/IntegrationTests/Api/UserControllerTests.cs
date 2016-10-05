using System.Threading.Tasks;
using System.Web.Http.Results;

using Xunit;
using Xunit.Abstractions;

using Foundatio.Skeleton.Api.Controllers;
using Foundatio.Skeleton.Api.Models.Auth;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;

namespace Foundatio.Skeleton.IntegrationTests.API {
    public class UserControllerControllerTests : IntegrationTestsBase {
        private readonly UserController _userController;

        public UserControllerControllerTests(ITestOutputHelper output) : base(output) {
            _userController = GetService<UserController>();
            _userController.Request = CreateTestUserRequestAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task API_verify_email_marks_user_verified() {
            var userRepo = GetService<IUserRepository>();
            var user = new User {
                IsActive = true,
                FullName = "derek",
                EmailAddress = "derek@derek.com",
                IsEmailAddressVerified = false,
            };
            user.Salt = StringUtils.GetRandomString(16);
            user.Password = StringUtils.GetRandomString(16).ToSaltedHash(user.Salt);
            user.CreateVerifyEmailAddressToken();

            await userRepo.AddAsync(user);

            RefreshData();

            var res = await _userController.Verify(user.VerifyEmailAddressToken);

            Assert.IsType<OkResult>(res);

            RefreshData();

            var pulledUser = await userRepo.GetByIdAsync(user.Id);

            Assert.True(pulledUser.IsEmailAddressVerified);
        }

        // This thing also tests uniqueness across verified subdomains
        //[Fact]
        public async Task API_verify_email_verifies_org_too() {
            var userRepo = GetService<IUserRepository>();
            var orgRepo = GetService<IOrganizationRepository>();

            var org1 = new Organization {
                Name = "org",
                IsVerified = false,
            };

            var org2 = new Organization {
                Name = "org",
                IsVerified = false,
            };

            await orgRepo.AddAsync(org1);
            await orgRepo.AddAsync(org2);

            var user = new User {
                IsActive = true,
                FullName = "derek",
                EmailAddress = "derek@derek.com",
                IsEmailAddressVerified = false,

                Memberships = new Membership[] { new Membership { OrganizationId = org1.Id, Roles = new[] { "admin" } } },
            };
            user.Salt = StringUtils.GetRandomString(16);
            user.Password = StringUtils.GetRandomString(16).ToSaltedHash(user.Salt);
            user.CreateVerifyEmailAddressToken();

            var user2 = new User {
                IsActive = true,
                FullName = "derek",
                EmailAddress = "derek@derek.com",
                IsEmailAddressVerified = false,
                Memberships = new Membership[] { new Membership { OrganizationId = org2.Id, Roles = new[] { "admin" } } },
            };
            user2.Salt = StringUtils.GetRandomString(16);
            user2.Password = StringUtils.GetRandomString(16).ToSaltedHash(user.Salt);
            user2.CreateVerifyEmailAddressToken();

            await userRepo.AddAsync(user);
            await userRepo.AddAsync(user2);

            RefreshData();

            var res = _userController.Verify(user.VerifyEmailAddressToken);

            RefreshData();

            var org1Pulled = await orgRepo.GetByIdAsync(org1.Id);
            Assert.True(org1Pulled.IsVerified);

            var res2 = _userController.Verify(user2.VerifyEmailAddressToken);

            RefreshData();

            var org2Pulled = await orgRepo.GetByIdAsync(org2.Id);
            Assert.False(org2Pulled.IsVerified);
        }

        [Fact]
        public async Task API_verify_email_returns_token_when_null_password() {
            var userRepo = GetService<IUserRepository>();
            var user = new User {
                IsActive = true,
                FullName = "derek",
                EmailAddress = "derek@derek.com",
                IsEmailAddressVerified = false,
            };
            user.CreateVerifyEmailAddressToken();

            await userRepo.AddAsync(user);

            RefreshData();

            var res = await _userController.Verify(user.VerifyEmailAddressToken);

            Assert.IsType<OkNegotiatedContentResult<TokenResponseModel>>(res);
        }
    }
}
