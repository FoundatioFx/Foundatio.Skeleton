using System;

namespace Foundatio.Skeleton.Api.Models.Auth
{
    public class ChangeEmailAddressModel
    {
        public string CurrentPassword { get; set; }
        public string NewEmailAddress { get; set; }
    }
}