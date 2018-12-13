using System;
using System.Threading;

namespace NCamel.Core
{
    public static class PollerExtensions
    {
        public static Poller<T> FromPoller<T>(this Context ctx, TimeSpan minimumDelay)
        {
            var poller = new Poller<T>(minimumDelay, ctx.CancellationTokenSource.Token)
            {
                Ctx = ctx
            };
            ctx.Register(poller);

            return poller;
        }
    }

    public abstract class Producer<T>: IProducer<T>
    {
        public Context Ctx { get; set; }
        Route IProducer.Route
        {
            get => Route;
            set => Route = (Route<T>) value;
        }

        public Route<T> Route { get; set; }
        public abstract void Execute();

        protected Func<IProducer<T>> ProducerFac;

        public Route<T> To(Func<IProducer<T>> producerFac)
        {
            ProducerFac = producerFac;
            Route = new Route<T>();
            return Route;
        }

        public Route<T> To(IProducer<T> producer)
        {
            return To(() => producer);
        }
    }

    public class Poller<T> : Producer<T>
    {
        private readonly TimeSpan minimumDelay;
        private readonly CancellationToken cancellationToken;

        public Poller(TimeSpan minimumDelay, CancellationToken cancellationToken)
        {
            this.minimumDelay = minimumDelay;
            this.cancellationToken = cancellationToken;
        }

        public override void Execute()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                var producer = ProducerFac();
                producer.Route = Route;
                producer.Execute();
                producer.Ctx = Ctx;

                var duration = now - DateTime.Now;

                if (duration < minimumDelay)
                    cancellationToken.WaitHandle.WaitOne(minimumDelay - duration);
            }
        }
    }
}