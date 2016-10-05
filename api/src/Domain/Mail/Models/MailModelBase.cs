using System;

namespace Foundatio.Skeleton.Domain.Mail.Models {
    public class MailModelBase : IMailModel {
        public string BaseUrl { get; set; }
    }

    public interface IMailModel {
        string BaseUrl { get; set; }
    }
}
