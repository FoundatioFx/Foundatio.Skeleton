using System.Linq;
using Foundatio.Skeleton.Core.Extensions;
using System;
using System.Threading.Tasks;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Domain.Repositories;

namespace Foundatio.Skeleton.Domain.Services
{
    public class OrganizationService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IUserRepository _userRepository;

        public OrganizationService(IOrganizationRepository organizationRepository, IUserRepository userRepository, ITokenRepository tokenRepository)
        {
            _organizationRepository = organizationRepository;
            _userRepository = userRepository;
        }

        public virtual Task<string> AddInvitedUserToOrganizationAsync(string token, User user)
        {
            if (!Settings.Current.EnableAccountInvites)
                throw new ApplicationException("Account invites are currently disabled.");
            if (user == null)
                throw new ArgumentNullException(nameof(user), "user missing");

            if (!String.IsNullOrWhiteSpace(token))
                return AddInvitedUserToOrganizationByTokenAsync(token, user);

            throw new ApplicationException("must invite by invite token");
        }

        private async Task<string> AddInvitedUserToOrganizationByTokenAsync(string token, User user)
        {
            var result = await _organizationRepository.GetByInviteTokenAsync(token).AnyContext();
            if (result == null)
                throw new ApplicationException("invite lost");

            var saveUser = false;

            if (user.FullName == user.EmailAddress && !String.IsNullOrWhiteSpace(result.Item2.FullName)) {
                user.FullName = result.Item2.FullName;
                saveUser = true;
            }

            if (!user.IsEmailAddressVerified && String.Equals(user.EmailAddress, result.Item2.EmailAddress, StringComparison.OrdinalIgnoreCase)) {
                user.MarkEmailAddressVerified();
                saveUser = true;
            }

            //  added by user id was added after the fact
            if (String.IsNullOrEmpty(result.Item2.AddedByUserId)) {
                //  all prior invites can only have the user role without the added by verification
                result.Item2.Roles = AuthorizationRoles.UserScope;

            } else {
                var addedBy = await _userRepository.GetByIdAsync(result.Item2.AddedByUserId).AnyContext();

                if (addedBy == null)
                    throw new ApplicationException("the user that sent the invite was not found");
                
                //  this can and should only happen if the user adding the invite was a global admin
                if (result.Item2.Roles.Contains(AuthorizationRoles.GlobalAdmin, StringComparer.OrdinalIgnoreCase)) {
                    if (addedBy.IsGlobalAdmin()) {
                        if (user.AddedGlobalAdminRole()) {
                            saveUser = true;
                        } //  the else here means this user was already a global admin so do nothing

                        result.Item2.Roles = AuthorizationRoles.AdminScope;
                    } else if (addedBy.IsAdmin(result.Item1.Id)) {
                        result.Item2.Roles = AuthorizationRoles.AdminScope;
                    } else
                        result.Item2.Roles =  AuthorizationRoles.UserScope;

                } else if (result.Item2.Roles.Contains(AuthorizationRoles.Admin, StringComparer.OrdinalIgnoreCase)
                    && !addedBy.IsAdmin(result.Item1.Id))
                    result.Item2.Roles = AuthorizationRoles.UserScope;

            }

            if (user.AddedMembershipRoles(result.Item1.Id, result.Item2.Roles)) {
                saveUser = true;
            }

            if (String.IsNullOrEmpty(user.Id))
                await _userRepository.AddAsync(user);
            else if(saveUser)
                await _userRepository.SaveAsync(user);

            result.Item1.Invites.Remove(result.Item2);
            await _organizationRepository.SaveAsync(result.Item1).AnyContext();

            return result.Item1.Id;
        }


        public virtual async Task<bool> TryMarkOrganizationAsVerifiedAsync(string organizationId)
        {
            if (String.IsNullOrWhiteSpace(organizationId))
                return false;

            var organization = await _organizationRepository.GetByIdAsync(organizationId, true).AnyContext();

            if (organization.IsVerified)
                return false;

            organization.MarkVerified();
            await _organizationRepository.SaveAsync(organization).AnyContext();

            return true;
        }
    }
}
