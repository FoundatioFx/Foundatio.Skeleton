using System;

namespace Foundatio.Skeleton.Api.Models {
    public class SignupModel {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string InviteToken { get; set; }
        public string OrganizationName { get; set; }
    }
}