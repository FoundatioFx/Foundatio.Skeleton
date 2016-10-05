using System;

namespace Foundatio.Skeleton.Api.Models {
    public class ExternalAuthInfo {
        public string Code { get; set; }
        public string RedirectUri { get; set; }
        public string InviteToken { get; set; }
    }
}