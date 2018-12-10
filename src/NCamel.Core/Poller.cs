using System;
using System.Threading;

namespace NCamel.Core
{
    public static class PollerExtensions
    {
        public static Poller FromPoller(this Context c, TimeSpan minimumDelay)
        {
            var poller = new Poller(minimumDelay, c.CancellationTokenSource.Token);
            c.Register(poller);
            return poller;
        }
    }

    public class Poller : IProducer
    {
        private readonly TimeSpan minimumDelay;
        private readonly CancellationToken cancellationToken;
        private Action f;

        public Poller(TimeSpan minimumDelay, CancellationToken cancellationToken)
        {
            this.minimumDelay = minimumDelay;
            this.cancellationToken = cancellationToken;
        }

        public void To(Action f)
        {
            this.f = f;
        }

        public void Execute()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                f();

                var duration = now - DateTime.Now;

                if (duration < minimumDelay)
                    cancellationToken.WaitHandle.WaitOne(minimumDelay - duration);
            }
        }
    }
}