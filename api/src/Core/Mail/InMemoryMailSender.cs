using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Skeleton.Core.Queues.Models;

namespace Foundatio.Skeleton.Core.Mail
{
    public class InMemoryMailSender : IMailSender, IDisposable
    {        
        private readonly Queue<MailMessage> _recentMessages = new Queue<MailMessage>();
        private long _totalSent;
        private readonly EventWaitHandle _waitHandle = new AutoResetEvent(false);

        public InMemoryMailSender()
        {
            MessagesToStore = 25;
        }

        public void WaitForSend(long count = 1, double timeoutInSeconds = 10, Action work = null)
        {
            if (count == 0)
                return;

            long currentCount = _totalSent;
            if (work != null)
                work();

            count = count - (_totalSent - currentCount);

            do
            {
                if (!_waitHandle.WaitOne(TimeSpan.FromSeconds(timeoutInSeconds)))
                    throw new TimeoutException();

                count--;
            } while (count > 0);
        }

        public int MessagesToStore { get; set; }
        public long TotalSent { get { return _totalSent; } }
        public List<MailMessage> SentMessages { get { return _recentMessages.ToList(); } }
        public MailMessage LastMessage { get { return SentMessages.Last(); } }

        public Task SendAsync(MailMessage model)
        {
            _recentMessages.Enqueue(model);
            Interlocked.Increment(ref _totalSent);
            _waitHandle.Set();

            while (_recentMessages.Count > MessagesToStore)
                _recentMessages.Dequeue();

            return Task.FromResult(0);
        }

        public void Dispose() {
            if (_waitHandle != null)
                _waitHandle.Dispose();
        }
    }
}
