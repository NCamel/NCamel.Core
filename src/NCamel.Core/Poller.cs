using System;
using System.Threading;

namespace NCamel.Core
{
    public static class PollerExtensions
    {
        public static Poller FromPoller(this Context c, TimeSpan minimumDelay)
        {
            var poller = new Poller(minimumDelay, c);
            c.Register(poller);
            return poller;
        }
    }

    public class Poller : IProducer
    {
        private readonly TimeSpan minimumDelay;
        private readonly Context ctx;
        private readonly CancellationToken cancellationToken;
        private Func<IProducer> producerFac;

        public Poller(TimeSpan minimumDelay, Context ctx)
        {
            this.minimumDelay = minimumDelay;
            this.ctx = ctx;
            this.cancellationToken = ctx.CancellationTokenSource.Token;
        }

        public Route To(Func<IProducer> producerFac)
        {
            this.producerFac = producerFac;
            Route = new Route(ctx, null);
            return Route;
        }

        public Route Route { get; set; }

        public void Execute()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                var producer = producerFac();
                producer.Route = Route;
                producer.Execute();

                var duration = now - DateTime.Now;

                if (duration < minimumDelay)
                    cancellationToken.WaitHandle.WaitOne(minimumDelay - duration);
            }
        }
    }
}