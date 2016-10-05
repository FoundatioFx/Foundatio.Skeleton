using System;

namespace Foundatio.Skeleton.Api.Models {
    public class InviteUserResponse {
        public bool Added { get; set; }
        public bool Invited { get; set; }
        public string UserId { get; set; }
        public string EmailAddress { get; set; }
    }
}