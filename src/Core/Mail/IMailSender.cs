using System;
using System.Threading.Tasks;
using Foundatio.Skeleton.Core.Queues.Models;

namespace Foundatio.Skeleton.Core.Mail {
    public interface IMailSender
    {
        Task SendAsync(MailMessage model);
    }
}
