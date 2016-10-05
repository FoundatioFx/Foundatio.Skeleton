using System;
using System.Threading.Tasks;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Domain.Services
{
    public class SampleDataService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;

        public const string TEST_USER_EMAIL = "test@foundatio.com";
        public const string TEST_USER_PASSWORD = "tester";
        public const string TEST_ORG_ID = "537650f3b77efe23a47914f3";
        public const string TEST_API_KEY = "LhhP1C9gijpSKCslHHCvwdSIz298twx271n1l6xw";
        public const string TEST_USER_API_KEY = "5f8aT5j0M1SdWCMOiJKCrlDNHMI38LjCH4LTWqGp";

        public const string ANOTHER_USER_EMAIL = "anothertest@foundatio.com";
        public const string ANOTHER_USER_PASSWORD = "another";
        public const string ANOTHER_ORG_ID = "5788f9ff914cf51ea0befa65";
        public const string ANOTHER_API_KEY = "5788f9ff914cf51ea0befa655788f9ff914cf51ea0befa65";
        public const string ANOTHER_USER_API_KEY = "5788fa10914cf51ea0befa665788fa10914cf51ea0befa66";

        public const string FOREIGN_USER_EMAIL = "foreign@foundatio.com";
        public const string FOREIGN_USER_PASSWORD = "foreigner";
        public const string FOREIGN_ORG_ID = "55300f25553d53187c16ff10";
        public const string FOREIGN_API_KEY = "55300f25553d53187c10vmnmgYxi8VIKhb6t1rjS";
        public const string FOREIGN_USER_API_KEY = "dRAKxiWg8mFcFx5MVBYTMBlTTghdAWAYdegjBq7g";

        public SampleDataService(IOrganizationRepository organizationRepository, IUserRepository userRepository, ITokenRepository tokenRepository)
        {
            _organizationRepository = organizationRepository;
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
        }

        public async Task CreateTestDataAsync()
        {
            var testUser = await CreateTestUserAsync(TEST_USER_EMAIL, "Test User", TEST_USER_PASSWORD).AnyContext();
            var anotherUser = await CreateTestUserAsync(ANOTHER_USER_EMAIL, "Another User", ANOTHER_USER_PASSWORD).AnyContext();
            var foreignUser = await CreateTestUserAsync(FOREIGN_USER_EMAIL, "Foreign User", FOREIGN_USER_PASSWORD).AnyContext();

            if (testUser != null)
                await CreateTestOrganizationAsync(testUser.Id, TEST_ORG_ID, "Acme", TEST_API_KEY, TEST_USER_API_KEY).AnyContext();

            if (anotherUser != null)
                await CreateTestOrganizationAsync(anotherUser.Id, ANOTHER_ORG_ID, "Another Acme", ANOTHER_API_KEY, ANOTHER_USER_API_KEY).AnyContext();

            if (foreignUser != null)
                await CreateTestOrganizationAsync(foreignUser.Id, FOREIGN_ORG_ID, "ForeignCo", FOREIGN_API_KEY, FOREIGN_USER_API_KEY).AnyContext();
        }

        public async Task<User> CreateTestUserAsync(string userEmail, string userFullName, string password, string imagePath = null)
        {
            if (await _userRepository.GetByEmailAddressAsync(userEmail).AnyContext() != null)
                return null;

            var user = new User
            {
                FullName = userFullName,
                EmailAddress = userEmail,
                IsEmailAddressVerified = true,
                ProfileImagePath = imagePath,
            };
            user.Roles.Add(AuthorizationRoles.GlobalAdmin);

            user.Salt = StringUtils.GetRandomString(16);
            user.Password = password.ToSaltedHash(user.Salt);

            user = await _userRepository.AddAsync(user).AnyContext();
            return user;
        }

        public async Task CreateTestOrganizationAsync(string userId, string organizationId, string organizationName, string apiKey, string userApiKey)
        {
            if (await _tokenRepository.GetByIdAsync(apiKey).AnyContext() != null)
                return;

            var user = await _userRepository.GetByIdAsync(userId, true).AnyContext();
            var organization = new Organization { Id = organizationId, Name = organizationName };

            organization = await _organizationRepository.AddAsync(organization).AnyContext();

            await _tokenRepository.AddAsync(new Token
            {
                Id = apiKey,
                OrganizationId = organization.Id,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                Type = TokenType.Access
            }).AnyContext();

            await _tokenRepository.AddAsync(new Token
            {
                Id = userApiKey,
                UserId = user.Id,
                OrganizationId = organization.Id,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                Type = TokenType.Access
            }).AnyContext();

            user.AddAdminMembership(organization.Id);
            await _userRepository.SaveAsync(user, true).AnyContext();
        }
    }
}
