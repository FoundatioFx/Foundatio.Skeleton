using System;

namespace GoodProspect.Core.Mail.Models {
    public class MailModelBase : IMailModel {
        public string BaseUrl { get; set; }
    }

    public interface IMailModel {
        string BaseUrl { get; set; }
    }
}