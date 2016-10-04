using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Results;

using Foundatio.Queues;
using Xunit;

using Foundatio.Skeleton.Api.Controllers;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Api.Models.Auth;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Queues.Models;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;
using Xunit.Abstractions;

namespace Foundatio.Skeleton.IntegrationTests.API {
    public class AuthControllerTests : IntegrationTestsBase {
        private readonly AuthController _authController;

        public AuthControllerTests(ITestOutputHelper output) : base(output) {
            _authController = GetService<AuthController>();

            ResetAllAsync().GetAwaiter().GetResult();
        }

        [Fact]
        public async Task Signup_should_create_and_add_to_org() {
            var userRepo = GetService<IUserRepository>();
            var orgRepo = GetService<IOrganizationRepository>();
            var tokenRepo = GetService<ITokenRepository>();

            var result = await _authController.Signup(new SignupModel {
                Email = "email@email.com",
                Name = "hello",
                Password = "P@ssword",
                OrganizationName = "Org Name Here",
            }) as OkNegotiatedContentResult<TokenResponseModel>;

            RefreshData();

            var token = await tokenRepo.GetByIdAsync(result.Content.Token);
            var userId = token.UserId;
            var orgId = token.OrganizationId;

            var user = await userRepo.GetByIdAsync(userId);
            var org = await orgRepo.GetByIdAsync(orgId);

            Assert.Equal(orgId, user.Memberships.First().OrganizationId);
            Assert.Equal(orgId, org.Id);
            Assert.Equal("Org Name Here", org.Name);
        }

        [Fact]
        public async Task Signup_ShouldNotAllowNoPassword() {
            var result = await _authController.Signup(new SignupModel {
                Email = "igotnopassword@email.com",
                Name = "hello",
                OrganizationName = "Org Name Here",
            }) as BadRequestErrorMessageResult;

            Assert.NotNull(result);
        }

        [Fact]
        public async Task API_signup_should_not_return_token_on_existing_email_address_and_no_password() {
            var userRepo = GetService<IUserRepository>();

            var emailAddress = "sample@domain.org";
            var password = "P@ssword1";
            var salt = StringUtils.GetRandomString(16);

            var existingUser = new User {
                EmailAddress = emailAddress,
                Password = password.ToSaltedHash(salt),
                Salt = salt,
                FullName = "The Dude",
                IsActive = true,
                IsEmailAddressVerified = true,
                Memberships = new Membership[] { },
            };

            await userRepo.AddAsync(existingUser);

            RefreshData();

            var resNoPassword = await _authController.Signup(new SignupModel {
                Email = emailAddress,
                Name = "powpow",
            });

            Assert.IsType<BadRequestErrorMessageResult>(resNoPassword);

            var resWrongPassword = await _authController.Signup(new SignupModel {
                Email = emailAddress,
                Name = "powpow",
                Password = "wrong",
            });

            Assert.IsType<BadRequestErrorMessageResult>(resWrongPassword);
        }

        [Fact]
        public async Task API_signup_with_invite_token_succeeds() {
            var tokenRepo = GetService<ITokenRepository>();
            var userRepo = GetService<IUserRepository>();
            var orgRepo = GetService<IOrganizationRepository>();
            var mailQueue = GetService<IQueue<MailMessage>>() as InMemoryQueue<MailMessage>;

            var emailAddress = "sample@domain.org";
            var fullName = "The Dude";
            var password = "P@ssword1";
            var inviteToken = StringUtils.GetNewToken();

            var organization = new Organization {
                Name = "Sample Organization",
                IsVerified = true,
                Invites = new Collection<Invite>
                {
                    new Invite
                    {
                        EmailAddress = emailAddress,
                        DateAdded = DateTime.Now,
                        Token = inviteToken,
                        Roles = new Collection<string> { AuthorizationRoles.User }
                    }
                }
            };

            await orgRepo.AddAsync(organization);

            //

            RefreshData();

            var result = await _authController.Signup(new SignupModel {
                Email = emailAddress,
                Name = fullName,
                Password = password,
                InviteToken = inviteToken
            }) as OkNegotiatedContentResult<TokenResponseModel>;

            RefreshData();

            //

            Assert.NotNull(result);
            Assert.NotNull(result?.Content);
            Assert.NotEmpty(result?.Content.Token);

            var user = await userRepo.GetByEmailAddressAsync(emailAddress);
            Assert.NotNull(user);
            Assert.Equal(fullName, user.FullName);
            Assert.NotEmpty(user.Memberships);
            Assert.True(user.IsEmailAddressVerified);
            Assert.Equal(password.ToSaltedHash(user.Salt), user.Password);

            var membership = user.Memberships.FirstOrDefault(m => m.OrganizationId == organization.Id);
            Assert.NotNull(membership);
            Assert.Contains(AuthorizationRoles.User, membership?.Roles);

            organization = await orgRepo.GetByIdAsync(organization.Id);
            Assert.Empty(organization.Invites);

            var token = await tokenRepo.GetByIdAsync(result?.Content.Token);
            Assert.NotNull(token);
            Assert.Equal(user.Id, token.UserId);
            Assert.Equal(organization.Id, token.OrganizationId);
            Assert.Equal(TokenType.Access, token.Type);

            Assert.Equal(0, (await mailQueue.GetQueueStatsAsync()).Enqueued);
        }

        [Fact]
        public async Task API_login_with_creds() {
            var tokenRepo = GetService<ITokenRepository>();
            var userRepo = GetService<IUserRepository>();
            var orgRepo = GetService<IOrganizationRepository>();

            var emailAddress = "sample@domain.org";
            var password = "p@ssword";
            var salt = StringUtils.GetRandomString(16);

            var org = new Organization {
                Name = "Sample Organization"
            };

            await orgRepo.AddAsync(org);

            var existingUser = new User {
                EmailAddress = emailAddress,
                Password = password.ToSaltedHash(salt),
                Salt = salt,
                FullName = "The Dude",
                IsActive = true,
                IsEmailAddressVerified = true,
                Memberships = new[] { new Membership { OrganizationId = org.Id, Roles = new[] { "Admin" } } }
            };

            await userRepo.AddAsync(existingUser);

            //

            RefreshData();

            var result = await _authController.Login(new LoginModel {
                Email = emailAddress,
                Password = password,
            }) as OkNegotiatedContentResult<TokenResponseModel>;

            RefreshData();

            //

            Assert.NotNull(result);
            Assert.NotNull(result.Content);
            Assert.NotEmpty(result.Content.Token);

            var token = await tokenRepo.GetByIdAsync(result.Content.Token);
            Assert.Equal(token.OrganizationId, existingUser.Memberships.First().OrganizationId);
            Assert.NotNull(token);

            var user = await userRepo.GetByIdAsync(token.UserId);
            Assert.NotNull(user);
            Assert.Equal(emailAddress, user.EmailAddress);
        }

        [Fact]
        public async Task API_switch_organizations() {
            var tokenRepo = GetService<ITokenRepository>();
            var userRepo = GetService<IUserRepository>();
            var orgRepo = GetService<IOrganizationRepository>();

            var emailAddress = "sample@domain.org";
            var fullName = "The Dude";
            var password = "p@ssword";
            var token = StringUtils.GetNewToken();
            var salt = StringUtils.GetRandomString(16);

            var org1 = new Organization {
                Name = "Sample Organization1"
            };
            var org2 = new Organization {
                Name = "Sample Organization2"
            };
            var org3 = new Organization {
                Name = "Sample Organization3"
            };

            await orgRepo.AddAsync(new[] { org1, org2, org3 });

            var user = new User {
                EmailAddress = emailAddress,
                FullName = fullName,
                IsEmailAddressVerified = true,
                Password = password.ToSaltedHash(salt),
                Salt = salt,
                Memberships = new List<Membership> {
                    new Membership { OrganizationId = org1.Id, Roles = new List<string> { "role1", "role2" }},
                    new Membership { OrganizationId = org2.Id, Roles = new List<string> { "role2", "role3" }},
                }
            };

            await userRepo.AddAsync(user);

            await tokenRepo.AddAsync(new Token {
                UserId = user.Id,
                OrganizationId = org1.Id,
                CreatedBy = "someguy",
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(2),
                UpdatedUtc = DateTime.UtcNow,
                Type = TokenType.Access,
                Id = token
            });

            //

            RefreshData();

            _authController.Request = await CreateRequestAsync(user, org1.Id);
            var result = await _authController.SwitchOrganization(org2.Id) as OkNegotiatedContentResult<TokenResponseModel>;

            RefreshData();
            //

            Assert.NotNull(result);
            Assert.NotNull(result?.Content);
            Assert.NotEmpty(result?.Content.Token);

            Assert.NotEqual(token, result?.Content.Token);

            _authController.Request = await CreateRequestAsync(user, org2.Id);
            var result2 = await _authController.SwitchOrganization(org3.Id) as UnauthorizedResult;

            Assert.NotNull(result2);
        }
    }
}
