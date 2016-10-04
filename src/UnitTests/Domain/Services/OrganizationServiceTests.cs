using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FakeItEasy;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Services;
using Xunit.Abstractions;

namespace Foundatio.Skeleton.UnitTests.Domain.Services
{
    public class OrganizationServiceTests : UnitTestsBase
    {

        [Fact]
        public async void AddInvitedUserToOrganization_should_throw_when_token_is_missing()
        {
            // arrange
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();

            // act with assert
            await Assert.ThrowsAsync<ApplicationException>(async () =>
            {
                await service.AddInvitedUserToOrganizationAsync(null, new User());
            });
        }

        [Fact]
        public async void AddInvitedUserToOrganization_should_throw_when_null_user()
        {
            // arrange
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();

            // act with assert
            await Assert.ThrowsAsync<ArgumentNullException>( async () =>
            {
                await service.AddInvitedUserToOrganizationAsync("token", null);
            });
        }

        [Fact]
        public async void AddInvitedUserToOrganization_should_throw_when_org_not_found_for_invite()
        {
            // arrange
            var user = new User();
            var inviteToken = "invite-token";
            var invite = null as Invite;
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();
            var orgRepo = Container.GetInstance<IOrganizationRepository>();
            A.CallTo(() => orgRepo.GetByInviteTokenAsync(inviteToken)).Returns(Task.FromResult<Tuple<Organization, Invite>>(null));

            // act with assert
            await Assert.ThrowsAsync<ApplicationException>(async () =>
            {
                await service.AddInvitedUserToOrganizationAsync(inviteToken, user);
            });
        }


        [Fact]
        public async void AddInvitedUserToOrganization_should_mark_email_as_verified_if_not_verified()
        {
            // arrange
            var user = new User { EmailAddress = "email@address.com", IsEmailAddressVerified = false };
            var invite = new Invite { EmailAddress = user.EmailAddress };
            var organization = new Organization { IsVerified = true };
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();
            var orgRepo = Container.GetInstance<IOrganizationRepository>();
            var userRepo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => orgRepo.GetByInviteTokenAsync("token")).Returns(new Tuple<Organization, Invite>(organization, invite));

            // act
            await service.AddInvitedUserToOrganizationAsync("token", user);

            // assert
            A.CallTo(() => userRepo.AddAsync(user, false, null, true)).MustHaveHappened();
        }

        [Fact]
        public async void AddInvitedUserToOrganization_should_add_user_membership_if_not_in_org()
        {
            // arrange
            var orgId = "org-id";
            var emailAddress = "email@address.com";
            var roles = AuthorizationRoles.UserScope;
            var user = new User { EmailAddress = emailAddress };
            var invite = new Invite { EmailAddress = emailAddress, Roles = roles };
            var organization = new Organization { Id = orgId, IsVerified = true };
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();
            var orgRepo = Container.GetInstance<IOrganizationRepository>();
            var userRepo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => orgRepo.GetByInviteTokenAsync("token")).Returns(new Tuple<Organization, Invite>(organization, invite));

            // act
            await service.AddInvitedUserToOrganizationAsync("token", user);

            // assert
            A.CallTo(() => userRepo.AddAsync(A<User>.That.Matches(u => u.Memberships.Any(m => m.OrganizationId == orgId && m.Roles.Any(r => r == AuthorizationRoles.User))), false, null, true)).MustHaveHappened();
        }

        [Fact]
        public async void AddInvitedUserToOrganization_should_remove_invite_from_org_when_valid()
        {
            // arrange
            var token = "token";
            var emailAddress = "email@address.com";
            var user = new User { EmailAddress = emailAddress };
            var invite = new Invite { EmailAddress = emailAddress };
            var organization = new Organization { Id = "org-id", IsVerified = true, Invites = new Collection<Invite>(new List<Invite> { invite }) };
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();
            var orgRepo = Container.GetInstance<IOrganizationRepository>();
            A.CallTo(() => orgRepo.GetByInviteTokenAsync(token)).Returns(new Tuple<Organization, Invite>(organization, invite));

            // act
            await service.AddInvitedUserToOrganizationAsync(token, user);

            // assert
            A.CallTo(() => orgRepo.SaveAsync(A<Organization>.That.Matches(u => !u.Invites.Any(m => m.Token == token)), false, null, true)).MustHaveHappened();
        }

        [Fact]
        public async void AddInvitedUserToOrganization_should_set_name_when_provided()
        {
            // arrange
            var token = "token";
            var emailAddress = "email@address.com";
            var user = new User { EmailAddress = emailAddress, FullName = emailAddress };
            var invite = new Invite { EmailAddress = emailAddress, FullName = "Full Name" };
            var organization = new Organization { Id = "org-id", IsVerified = true, Invites = new Collection<Invite>(new List<Invite> { invite }) };
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();
            var orgRepo = Container.GetInstance<IOrganizationRepository>();
            var userRepo = Container.GetInstance<IUserRepository>();

            A.CallTo(() => orgRepo.GetByInviteTokenAsync(token)).Returns(new Tuple<Organization, Invite>(organization, invite));

            // act
            await service.AddInvitedUserToOrganizationAsync(token, user);

            // assert
            A.CallTo(() => userRepo.AddAsync(A<User>.That.Matches(u => u.FullName == "Full Name"), false, null, true)).MustHaveHappened();
        }

        [Fact]
        public async void AddInvitedUserToOrganization_should_not_set_name_when_user_fullname_is_set()
        {
            // arrange
            var token = "token";
            var emailAddress = "email@address.com";
            var user = new User { EmailAddress = emailAddress, FullName = "Don't Set Me" };
            var invite = new Invite { EmailAddress = emailAddress, FullName = "Full Name" };
            var service = Container.GetInstanceWithFakeDependencies<OrganizationService>();
            var orgRepo = Container.GetInstance<IOrganizationRepository>();
            var userRepo = Container.GetInstance<IUserRepository>();

            var organization = new Organization { Id = "org-id", IsVerified = true, Invites = new Collection<Invite>(new List<Invite> { invite }) };
            A.CallTo(() => orgRepo.GetByInviteTokenAsync(token)).Returns(new Tuple<Organization, Invite>(organization, invite));

            // act
            await service.AddInvitedUserToOrganizationAsync(token, user);

            // assert
            A.CallTo(() => userRepo.SaveAsync(A<User>.That.Matches(u => u.FullName == "Full Name"), false, null, true)).MustNotHaveHappened();
        }

        public OrganizationServiceTests(ITestOutputHelper output) : base(output) {}
    }

}
