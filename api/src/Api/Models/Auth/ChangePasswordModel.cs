using System;

namespace Foundatio.Skeleton.Api.Models {
    public class ChangePasswordModel {
        public string CurrentPassword { get; set; }
        public string Password { get; set; }
    }
}