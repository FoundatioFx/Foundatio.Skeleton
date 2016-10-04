using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Exceptionless;
using Exceptionless.DateTimeExtensions;
using FluentValidation;
using Foundatio.Skeleton.Api.Extensions;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;
using Foundatio.Skeleton.Domain.Services;
using Foundatio.Logging;
using Foundatio.Skeleton.Api.Models;
using Foundatio.Skeleton.Api.Models.Auth;
using Foundatio.Skeleton.Api.Utility;
using Foundatio.Skeleton.Domain;
using OAuth2.Client;
using OAuth2.Client.Impl;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using Swashbuckle.Swagger.Annotations;

namespace Foundatio.Skeleton.Api.Controllers {
    [RoutePrefix(API_PREFIX + "/auth")]
    public class AuthController : AppApiController {
        private readonly IUserRepository _userRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ITemplatedMailService _mailer;
        private readonly ITokenRepository _tokenRepository;
        private readonly OrganizationService _organizationService;
        private readonly ILogger _logger;

        private static bool _isFirstUserChecked;
        private const string _invalidPasswordMessage = "The Password must be at least 8 characters long.";

        public AuthController(ILoggerFactory loggerFactory, IUserRepository userRepository, IOrganizationRepository orgRepository, ITemplatedMailService mailer, ITokenRepository tokenRepository, OrganizationService organizationService) {
            _logger = loggerFactory?.CreateLogger<AuthController>() ?? NullLogger.Instance;
            _userRepository = userRepository;
            _organizationRepository = orgRepository;
            _mailer = mailer;
            _tokenRepository = tokenRepository;
            _organizationService = organizationService;
        }

        /// <summary>
        /// Login with a local username and password.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>An auth token.</returns>
        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(TokenResponseModel))]
        [HttpPost]
        [Route("login")]
        public async Task<IHttpActionResult> Login(LoginModel model) {
            if (String.IsNullOrWhiteSpace(model?.Email))
                return BadRequest("Email Address is required.");

            if (String.IsNullOrWhiteSpace(model.Password))
                return BadRequest("Password is required.");

            User user;
            try {
                user = await _userRepository.GetByEmailAddressAsync(model.Email);
            } catch (Exception) {
                return Unauthorized();
            }

            if (user == null || !user.IsActive)
                return Unauthorized();

            if (!user.IsValidPassword(model.Password)) {
                return Unauthorized();
            }

            if (user.Memberships.Count == 0)
                return BadRequest("You must belong to at least one organization.");

            var organizationId = user.Memberships.First().OrganizationId;

            ExceptionlessClient.Default.CreateFeatureUsage("Login").AddObject(user).Submit();
            return Ok(new TokenResponseModel { Token = await GetToken(user, organizationId) });
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(TokenResponseModel))]
        [HttpPost]
        [Route("signup")]
        public async Task<IHttpActionResult> Signup(SignupModel model) {
            if (!Settings.Current.EnableAccountCreation)
                return BadRequest("Sorry, Foundatio Skeleton is not accepting new accounts at this time.");

            if (String.IsNullOrWhiteSpace(model?.Email))
                return BadRequest("Email address is required.");

            if (String.IsNullOrWhiteSpace(model?.Password))
                return BadRequest("Password is required.");

            User user;

            try {
                user = await _userRepository.GetByEmailAddressAsync(model.Email);
            } catch (Exception ex) {
                ex.ToExceptionless().MarkAsCritical().AddTags("Signup").AddObject(model).Submit();
                return BadRequest("An error occurred.");
            }

            if (user == null) {
                user = new User {
                    IsActive = true,
                    FullName = model.Name ?? model.Email,
                    EmailAddress = model.Email,
                    IsEmailAddressVerified = false
                };
                user.CreateVerifyEmailAddressToken();
                await AddGlobalAdminRoleIfFirstUser(user);

                if (!IsValidPassword(model.Password))
                    return BadRequest(_invalidPasswordMessage);

                user.Salt = StringUtils.GetRandomString(16);
                user.Password = model.Password.ToSaltedHash(user.Salt);
            } else {
                if (String.IsNullOrEmpty(model.InviteToken)) {
                    // if user is already there, password required.
                    // TODO: check if invite token.
                    // TODO: THERE ARE SO MANY THINGS TO DO HERE!
                    // we are just preventing rogue 'logins' through signup

                    // if the password is correct, just log them in.
                    if (user.IsValidPassword(model.Password) == false) {
                        return BadRequest("It looks like you already have an account on Foundatio Skeleton.");
                    }
                }
            }

            string organizationId = null;

            if (!String.IsNullOrWhiteSpace(model.InviteToken)) {
                organizationId = await _organizationService.AddInvitedUserToOrganizationAsync(model.InviteToken, user);
            }
            else if (!String.IsNullOrWhiteSpace(model.OrganizationName))
            {
                var newOrg = new Organization
                {
                    Name = model.OrganizationName,
                };

                await _organizationRepository.AddAsync(newOrg);

                organizationId = newOrg.Id;
                user.AddAdminMembership(newOrg.Id);

                if (String.IsNullOrEmpty(user.Id)) {
                    try {
                        user = await _userRepository.AddAsync(user);
                    } catch (ValidationException ex) {
                        return BadRequest(String.Join(", ", ex.Errors));
                    } catch (Exception ex) {
                        ex.ToExceptionless().MarkAsCritical().AddTags("Signup").AddObject(user).AddObject(model).Submit();
                        return BadRequest("An error occurred.");
                    }
                } else
                    await _userRepository.SaveAsync(user);
            }

            if (!user.IsEmailAddressVerified)
                _mailer.SendVerifyEmail(user);

            ExceptionlessClient.Default.CreateFeatureUsage("Signup").AddObject(user).Submit();
            return Ok(new TokenResponseModel { Token = await GetToken(user, organizationId) });
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost]
        [Route("google")]
        public Task<IHttpActionResult> Google(ExternalAuthInfo value) {
            return ProcessOAuthClient(value, Settings.Current.GoogleAppId, Settings.Current.GoogleAppSecret, (f, c) => new GoogleClient(f, c));
        }

        //[HttpPost]
        //[Route("facebook")]
        //public IHttpActionResult Facebook(JObject value) {
        //    return ProcessOAuthClient(value, Settings.Current.FacebookAppId, Settings.Current.FacebookAppSecret, (f, c) => new FacebookClient(f, c));
        //}

        //[HttpPost]
        //[Route("live")]
        //public IHttpActionResult Live(JObject value) {
        //    return ProcessOAuthClient(value, Settings.Current.MicrosoftAppId, Settings.Current.MicrosoftAppSecret, (f, c) => new WindowsLiveClient(f, c));
        //}

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost]
        [Route("unlink/{providerName:minlength(1)}")]
        [Authorize(Roles = AuthorizationRoles.User)]
        public async Task<IHttpActionResult> RemoveExternalLogin(string providerName, [NakedBody] string providerUserId) {
            if (String.IsNullOrEmpty(providerName) || String.IsNullOrEmpty(providerUserId))
                return BadRequest("Invalid Provider Name or Provider User Id.");

            if (CurrentUser.OAuthAccounts.Count <= 1 && String.IsNullOrEmpty(CurrentUser.Password))
                return BadRequest("You must set a local password before removing your external login.");

            if (CurrentUser.RemoveOAuthAccount(providerName, providerUserId))
                await _userRepository.SaveAsync(CurrentUser);

            ExceptionlessClient.Default.CreateFeatureUsage("Remove External Login").AddTags(providerName).AddObject(CurrentUser).Submit();
            return Ok();
        }

        [HttpPost]
        [Route("change-email-address")]
        [Authorize(Roles = AuthorizationRoles.User)]
        public async Task<IHttpActionResult> ChangeEmailAddress(ChangeEmailAddressModel model) {
            if (model == null)
                return BadRequest();

            // User has a local account..
            if (!String.IsNullOrWhiteSpace(CurrentUser.Password)) {
                if (String.IsNullOrWhiteSpace(model.CurrentPassword))
                    return BadRequest("The current password is incorrect.");

                string encodedPassword = model.CurrentPassword.ToSaltedHash(CurrentUser.Salt);
                if (!String.Equals(encodedPassword, CurrentUser.Password))
                    return BadRequest("The current password is incorrect.");
            }

            var user = await _userRepository.GetByIdAsync(CurrentUser.Id);
            user.EmailAddress = model.NewEmailAddress;
            user.CreateVerifyEmailAddressToken();

            //  TODO(chad):  this could cause issues if a user creates an account and verifies the email address,
            //  TODO(cont.):     then changes the email address resulting in an org that has been verified with no users verified.
            user.IsEmailAddressVerified = false;

            await _userRepository.SaveAsync(user);
            _mailer.SendVerifyEmail(user);

            // TODO(derek): send to old email address too??

            ExceptionlessClient.Default.CreateFeatureUsage("Change Email Address").AddObject(CurrentUser).Submit();
            return Ok();
        }

        [HttpPost]
        [Route("change-password")]
        [Authorize(Roles = AuthorizationRoles.User)]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordModel model) {
            if (model == null || !IsValidPassword(model.Password))
                return BadRequest(_invalidPasswordMessage);

            // User has a local account..
            if (!String.IsNullOrWhiteSpace(CurrentUser.Password)) {
                if (String.IsNullOrWhiteSpace(model.CurrentPassword))
                    return BadRequest("The current password is incorrect.");

                string encodedPassword = model.CurrentPassword.ToSaltedHash(CurrentUser.Salt);
                if (!String.Equals(encodedPassword, CurrentUser.Password))
                    return BadRequest("The current password is incorrect.");
            }

            await ChangePassword(CurrentUser, model.Password);

            ExceptionlessClient.Default.CreateFeatureUsage("Change Password").AddObject(CurrentUser).Submit();
            return Ok();
        }

        [HttpPost]
        [Route("create-password")]
        [Authorize(Roles = AuthorizationRoles.User)]
        public async Task<IHttpActionResult> CreatePassword(CreatePasswordModel model) {
            // this will only work if current password is null (via signup flow)
            if (!String.IsNullOrWhiteSpace(CurrentUser.Password)) {
                return BadRequest("The password has already been created.");
            }

            if (model == null || !IsValidPassword(model.Password))
                return BadRequest(_invalidPasswordMessage);

            await ChangePassword(CurrentUser, model.Password);

            ExceptionlessClient.Default.CreateFeatureUsage("Create Password").AddObject(CurrentUser).Submit();
            return Ok();
        }

        [HttpGet]
        [Route("check-email-address/{email:minlength(1)}")]
        public async Task<IHttpActionResult> IsEmailAddressAvailable(string email) {
            email = email.Trim();

            if (String.IsNullOrWhiteSpace(email))
                return StatusCode(HttpStatusCode.NoContent);

            if (CurrentUser != null && String.Equals(CurrentUser.EmailAddress, email, StringComparison.OrdinalIgnoreCase))
                return StatusCode(HttpStatusCode.Created);

            if (await _userRepository.GetByEmailAddressAsync(email) == null)
                return StatusCode(HttpStatusCode.NoContent);

            return StatusCode(HttpStatusCode.Created);
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(TokenResponseModel))]
        [Authorize]
        [HttpGet]
        [Route("switch-organization")]
        public async Task<IHttpActionResult> SwitchOrganization(string organizationId) {
            if (string.IsNullOrEmpty(organizationId))
                return BadRequest("Invalid organizationId.");

            if (!CurrentUser.IsGlobalAdmin() && !CurrentUser.Memberships.Contains(m => m.OrganizationId == organizationId))
                return Unauthorized();

            var token = await _tokenRepository.GetOrCreateUserToken(CurrentUser.Id, organizationId);

            return Ok(new TokenResponseModel { Token = token.Id });
        }

        [HttpGet]
        [Route("forgot-password")]
        public async Task<IHttpActionResult> ForgotPassword(string emailAddress) {
            var email = new Email { Address = emailAddress };
            var validator = new Foundatio.Skeleton.Domain.Validators.EmailValidator();
            if (!validator.TryValidate(email))
                return BadRequest("Please specify a valid Email Address.");

            var user = await _userRepository.GetByEmailAddressAsync(email.Address);
            if (user != null) {
                ExceptionlessClient.Default.CreateFeatureUsage("Forgot Password").AddObject(user).Submit();

                if (user.PasswordResetTokenCreated < DateTime.UtcNow.SubtractDays(1)) {
                    user.PasswordResetToken = StringUtils.GetNewToken();
                    user.PasswordResetTokenCreated = DateTime.UtcNow;
                } else {
                    // keep the same token in case people request reset multiple times
                    user.PasswordResetTokenCreated = DateTime.UtcNow;
                }
                await _userRepository.SaveAsync(user);

                _mailer.SendPasswordReset(user);
            } else {
                ExceptionlessClient.Default.CreateFeatureUsage("Forgot Password").AddObject(email).Submit();

                _mailer.SendPasswordResetEmailAddressNotFound(email.Address);
            }

            return Ok();
        }

        [HttpGet]
        [Route("verify-password-reset-token")]
        public async Task<IHttpActionResult> VerifyPasswordToken(string token) {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Invalid Password Reset Token.");

            var user = await _userRepository.GetByPasswordResetTokenAsync(token);

            if (!user.HasValidPasswordResetTokenExpiration())
                return NotFound();

            return Ok();
        }

        [SwaggerResponse(HttpStatusCode.OK, Type = typeof(TokenResponseModel))]
        [HttpPost]
        [Route("reset-password")]
        public async Task<IHttpActionResult> ResetPassword(ResetPasswordModel model) {
            if (String.IsNullOrEmpty(model?.PasswordResetToken))
                return BadRequest("Invalid Password Reset Token.");

            var user = await _userRepository.GetByPasswordResetTokenAsync(model.PasswordResetToken);
            if (user == null)
                return BadRequest("Invalid Password Reset Token.");

            if (!user.HasValidPasswordResetTokenExpiration())
                return BadRequest("Password Reset Token has expired.");

            if (!IsValidPassword(model.Password))
                return BadRequest(_invalidPasswordMessage);

            user.MarkEmailAddressVerified();
            await ChangePassword(user, model.Password);

            // TODO: use last logged in org
            var organizationId = user.Memberships.Count > 0 ? user.Memberships.First().OrganizationId : null;

            if (user.IsAdmin(organizationId))
                await _organizationService.TryMarkOrganizationAsVerifiedAsync(organizationId);

            ExceptionlessClient.Default.CreateFeatureUsage("Reset Password").AddObject(user).Submit();

            return Ok(new TokenResponseModel { Token = await GetToken(user, organizationId) });
        }

        [HttpPost]
        [Route("cancel-reset-password/{token:minlength(1)}")]
        public async Task<IHttpActionResult> CancelResetPassword(string token) {
            if (String.IsNullOrEmpty(token))
                return BadRequest("Invalid Password Reset Token.");

            var user = await _userRepository.GetByPasswordResetTokenAsync(token);
            if (user == null)
                return Ok();

            user.ResetPasswordResetToken();
            await _userRepository.SaveAsync(user);

            ExceptionlessClient.Default.CreateFeatureUsage("Cancel Reset Password").AddObject(user).Submit();
            return Ok();
        }

        private async Task AddGlobalAdminRoleIfFirstUser(User user) {
            if (_isFirstUserChecked)
                return;

            bool isFirstUser = await _userRepository.CountAsync() == 0;
            if (isFirstUser)
                user.AddGlobalAdminRole();

            _isFirstUserChecked = true;
        }

        private async Task<IHttpActionResult> ProcessOAuthClient<TClient>(ExternalAuthInfo authInfo, string appId, string appSecret, Func<IRequestFactory, IClientConfiguration, TClient> clientGenerator) where TClient : OAuth2Client {
            if (String.IsNullOrEmpty(authInfo?.Code)) {
                return NotFound();
            }

            if (String.IsNullOrEmpty(appId) || String.IsNullOrEmpty(appSecret)) {
                return NotFound();
            }

            var client = clientGenerator(new RequestFactory(), new RuntimeClientConfiguration {
                ClientId = appId,
                ClientSecret = appSecret,
                RedirectUri = authInfo.RedirectUri,
            });

            UserInfo userInfo;
            try {
                userInfo = client.GetUserInfo(authInfo.Code);
            } catch (Exception ex) {
                _logger.Error(ex, "Unable to get user info.");
                return BadRequest("Unable to get user info.");
            }

            LoginContext loginContext;
            try {
                loginContext = await AddExternalLogin(userInfo, authInfo.InviteToken);
            } catch (ApplicationException) {
                return BadRequest("Account Creation is currently disabled.");
            } catch (Exception ex) {
                _logger.Error(ex, "An error occurred while processing user info.");
                return BadRequest("An error occurred while processing user info.");
            }

            if (loginContext?.User == null)
                return BadRequest("Unable to process user info.");

            return Ok(new TokenResponseModel { Token = await GetToken(loginContext.User, loginContext.OrganizationId) });
        }

        private async Task<LoginContext> AddExternalLogin(UserInfo userInfo, string inviteToken) {
            ExceptionlessClient.Default.CreateFeatureUsage("External Login").AddTags(userInfo.ProviderName).AddObject(userInfo).Submit();

            var existingUser = await _userRepository.GetUserByOAuthProviderAsync(userInfo.ProviderName, userInfo.Id);

            // Link user accounts.
            if (CurrentUser != null) {
                if (existingUser != null) {
                    if (existingUser.Id != CurrentUser.Id) {
                        // Existing user account is not the current user. Remove it and we'll add it to the current user below.
                        if (!existingUser.RemoveOAuthAccount(userInfo.ProviderName, userInfo.Id))
                            return null;

                        await _userRepository.SaveAsync(existingUser);
                    } else {
                        // User is already logged in.
                        return new LoginContext {
                            User = CurrentUser,
                            OrganizationId = GetSelectedOrganizationId()
                        };
                    }
                }

                // Add it to the current user if it doesn't already exist and save it.
                CurrentUser.AddOAuthAccount(userInfo.ProviderName, userInfo.Id, userInfo.Email);
                await _userRepository.SaveAsync(CurrentUser);
                return new LoginContext {
                    User = CurrentUser,
                    OrganizationId = GetSelectedOrganizationId()
                };
            }

            // Create a new user account or return an existing one.
            if (existingUser != null) {
                var existingOrganizationId = existingUser.Memberships.Count > 0 ? existingUser.Memberships.First().OrganizationId : null;
                if (!existingUser.IsEmailAddressVerified) {
                    existingUser.MarkEmailAddressVerified();
                    await _userRepository.SaveAsync(existingUser);

                    if (existingUser.IsAdmin(existingOrganizationId)) {
                        await _organizationService.TryMarkOrganizationAsVerifiedAsync(existingOrganizationId);
                    }
                }

                return new LoginContext {
                    User = existingUser,
                    OrganizationId = existingOrganizationId
                };
            }

            // Check to see if a user already exists with this email address.
            var user = !String.IsNullOrEmpty(userInfo.Email) ? await _userRepository.GetByEmailAddressAsync(userInfo.Email) : null;
            //var isNewUser = false;
            if (user == null) {
                if (!Settings.Current.EnableAccountCreation)
                    throw new ApplicationException("Account Creation is currently disabled.");

                user = new User { FullName = userInfo.GetFullName(), EmailAddress = userInfo.Email };
                user.Roles.AddRange(AuthorizationRoles.UserScope);
                await AddGlobalAdminRoleIfFirstUser(user);

                //isNewUser = true;
            }

            user.MarkEmailAddressVerified();
            user.AddOAuthAccount(userInfo.ProviderName, userInfo.Id, userInfo.Email);
            await _userRepository.SaveAsync(user);

            string organizationId;
            if (!String.IsNullOrEmpty(inviteToken)) {
                organizationId = await _organizationService.AddInvitedUserToOrganizationAsync(inviteToken, user);
            }
            //else if (isNewUser)
            //{
            //    organizationId = _organizationService.CreateDefaultOrganization(user);
            //}
            else {
                organizationId = user.Memberships.Count > 0 ? user.Memberships.First().OrganizationId : null;
            }

            if (user.IsAdmin(organizationId)) {
                await _organizationService.TryMarkOrganizationAsVerifiedAsync(organizationId);
            }

            return new LoginContext {
                User = user,
                OrganizationId = organizationId
            };
        }

        private Task ChangePassword(User user, string password) {
            if (String.IsNullOrEmpty(user.Salt))
                user.Salt = StringUtils.GetNewToken();

            user.Password = password.ToSaltedHash(user.Salt);
            user.ResetPasswordResetToken();
            return _userRepository.SaveAsync(user);
        }

        private async Task<string> GetToken(User user, string organizationId) {
            var token = await _tokenRepository.GetOrCreateUserToken(user.Id, organizationId);
            return token.Id;
        }

        // just 8 characters long for now
        private static bool IsValidPassword(string password) {
            if (String.IsNullOrWhiteSpace(password))
                return false;

            return password.Length >= 8;
        }

        private class LoginContext {
            public User User { get; set; }

            public string OrganizationId { get; set; }
        }
    }
}
