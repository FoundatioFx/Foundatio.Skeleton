using System;
using System.Threading.Tasks;
using Xunit;
using Foundatio.Skeleton.Api.Controllers;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Domain.Repositories;
using FakeItEasy;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain.Services;
using System.Web.Http.Results;
using Xunit.Abstractions;

namespace Foundatio.Skeleton.UnitTests.Api.Controller {
    public class AuthControllerTests : UnitTestsBase {
        #region Login Tests

        [Fact]
        public async Task Login_should_return_bad_request_when_null_login() {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();

            // act
            var result = await controller.Login(null);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Theory]
        [InlineData(null)]
        //[InlineData("")]
        [InlineData(" ")]
        public async Task Login_should_return_bad_request_when_email_blank(string blankEmailAddress) {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();

            // act
            var loginArgs = new LoginModel { Email = blankEmailAddress };
            var result = await controller.Login(loginArgs);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Theory]
        [InlineData(null)]
        //[InlineData("")]
        [InlineData(" ")]
        public async Task Login_should_return_bad_request_when_password_blank(string blankPassword) {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();

            // act
            var loginArgs = new LoginModel { Email = "email@address.com", Password = blankPassword };
            var result = await controller.Login(loginArgs);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Fact]
        public async Task Login_should_return_unauthorized_when_user_repo_throws() {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var loginArgs = new LoginModel { Email = "email@address.com", Password = "password" };
            var userRepo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => userRepo.GetByEmailAddressAsync(loginArgs.Email)).Throws<ApplicationException>();

            // act
            var result = await controller.Login(loginArgs);

            // assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_should_return_unauthorized_when_user_is_null() {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var loginArgs = new LoginModel { Email = "email@address.com", Password = "password" };
            var userRepo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => userRepo.GetByEmailAddressAsync(loginArgs.Email)).Returns(Task.FromResult<User>(null));

            // act
            var result = await controller.Login(loginArgs);

            // assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_should_return_unauthorized_when_user_is_inactive() {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var loginArgs = new LoginModel { Email = "email@address.com", Password = "password" };
            var userRepo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => userRepo.GetByEmailAddressAsync(loginArgs.Email)).Returns(new User { IsActive = false });

            // act
            var result = await controller.Login(loginArgs);

            // assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Theory]
        [InlineData(null)]
        //[InlineData("")]
        [InlineData(" ")]
        public async Task Login_should_return_unauthorized_when_user_not_salty(string salt) // :)
        {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var loginArgs = new LoginModel { Email = "email@address.com", Password = "password" };
            var userRepo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => userRepo.GetByEmailAddressAsync(loginArgs.Email)).Returns(new User { IsActive = true, Salt = salt });

            // act
            var result = await controller.Login(loginArgs);

            // assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_should_return_unauthorized_when_password_does_not_match() {
            // arrange
            var enteredPassword = "mismatch-password";
            var realPassword = "real-password";
            var salt = "cwBvAG0AZQAtAHMAYQBsAHQA";
            var saltedPassword = realPassword.ToSaltedHash(salt);
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var loginArgs = new LoginModel { Email = "email@address.com", Password = enteredPassword };
            var userRepo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => userRepo.GetByEmailAddressAsync(loginArgs.Email)).Returns(new User { IsActive = true, Salt = salt, Password = saltedPassword });

            // act
            var result = await controller.Login(loginArgs);

            // assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion

        #region Signup Tests

        [Fact]
        public async Task Signup_should_return_bad_request_when_null_arg() {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();

            // act
            var result = await controller.Signup(null);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Theory]
        [InlineData(null)]
        //[InlineData("")]
        [InlineData(" ")]
        public async Task Signup_should_return_bad_request_when_email_blank(string blankEmailAddress) {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();

            // act
            var args = new SignupModel { Email = blankEmailAddress };
            var result = await controller.Signup(args);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Theory]
        [InlineData("1234567")]
        public async Task Signup_should_return_bad_request_when_password_invalid(string password) {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var repo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => repo.GetByEmailAddressAsync(A<string>.Ignored)).Returns(null as User);

            // act
            var args = new SignupModel { Email = Guid.NewGuid().ToString() + "@at.com", Name = "name", Password = password, InviteToken = "123" };
            var result = await controller.Signup(args);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Fact]
        public async Task Signup_should_return_bad_request_when_repo_check_existing_exception() {
            // arrange
            var email = "email@at.com";
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var repo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => repo.GetByEmailAddressAsync(email)).Throws<ApplicationException>();

            // act
            var args = new SignupModel { Email = email, Name = "name", Password = "password" };
            var result = await controller.Signup(args);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Fact]
        public async Task Signup_should_return_bad_request_when_repo_add_exception() {
            // arrange
            var email = "email@address.com";
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var repo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => repo.GetByEmailAddressAsync(email)).Returns(null as User);
            A.CallTo(() => repo.AddAsync(A<User>.Ignored, false, null, true)).Throws<ApplicationException>();

            // act
            var args = new SignupModel { Email = email, Name = "name", OrganizationName = "name", Password = "password" };
            var result = await controller.Signup(args);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }

        [Fact]
        public async Task Signup_should_create_verify_email_address_token() {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var repo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => repo.GetByEmailAddressAsync(A<string>.Ignored)).Returns(null as User);
            var service = Container.GetInstance<OrganizationService>();

            // act
            var args = new SignupModel { Email = "email@address.com", Name = "name", Password = "Password1", InviteToken = StringUtils.GetNewToken() };
            await controller.Signup(args);

            // assert
            A.CallTo(() => service.AddInvitedUserToOrganizationAsync(args.InviteToken, (A<User>.That.Matches(u => u.VerifyEmailAddressToken != null)))).MustHaveHappened();
        }

        [Fact]
        public async Task Signup_should_encrypt_password() {
            // arrange
            var password = "Password1";
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var repo = Container.GetInstance<IUserRepository>();
            A.CallTo(() => repo.GetByEmailAddressAsync(A<string>.Ignored)).Returns(null as User);
            var service = Container.GetInstance<OrganizationService>();

            // act
            var args = new SignupModel { Email = "email@address.com", Name = "name", Password = password, InviteToken = StringUtils.GetNewToken() };
            await controller.Signup(args);

            // assert
            A.CallTo(() => service.AddInvitedUserToOrganizationAsync(args.InviteToken, (A<User>.That.Matches(u => u.Salt != null && u.Password != password)))).MustHaveHappened();
        }

        [Fact]
        public async Task Signup_should_add_to_existing_when_invited() {
            // arrange
            var inviteToken = "invite-token!";
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();
            var userRepo = Container.GetInstance<IUserRepository>();
            var orgService = Container.GetInstance<OrganizationService>();
            A.CallTo(() => userRepo.GetByEmailAddressAsync(A<string>.Ignored)).Returns(null as User);

            // act
            var args = new SignupModel { InviteToken = inviteToken, Email = "email@address.com", Name = "name", Password = "Password1" };
            await controller.Signup(args);

            // assert
            A.CallTo(() => orgService.AddInvitedUserToOrganizationAsync(inviteToken, A<User>.Ignored)).MustHaveHappened();
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_should_return_bad_request_when_null() {
            // arrange
            var controller = Container.GetControllerWithFakeDepencencies<AuthController>();

            // act
            var result = await controller.ChangePassword(null);

            // assert
            Assert.IsType<BadRequestErrorMessageResult>(result);
        }


        #endregion

        public AuthControllerTests(ITestOutputHelper output) : base(output) {}
    }
}
