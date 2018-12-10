using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCamel.Core
{
    public interface IProducer<T>
    { }

    public class Context
    {
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public Action ExceptionHandling;

        private readonly List<Task> tasks = new List<Task>();

        public Context()
        {
            Logger.Info("Starting ctx");
        }

        public void Register(Action s)
        {
            tasks.Add(Task.Run(s).ContinueWith(Stop));
        }

        private void Stop(Task obj)
        {
            Logger.Info("Stopping");
        }

        public void RegisterRecurring(TimeSpan minimumDelay, Action f)
        {
            tasks.Add(Task.Run(() => new Throtler().Execute(minimumDelay, CancellationTokenSource.Token, f)));
        }
    }

    public class Throtler
    {
        public void Execute(TimeSpan minimumDelay, CancellationToken cancellationToken, Action f)
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

    public class Exchange
    {
        public Exchange(Context ctx, Action<Exchange> onComplete)
        {
            Ctx = ctx;
            OnCompleteHandler = onComplete;
        }

        public Context Ctx { get; }
        public DateTime StartTime { get; } = DateTime.Now;
        public Exception Exception { get; set; }

        public bool IsFaulted => Exception != null;

        public Message Message { get; set; }

        public Action<Exchange> OnCompleteHandler { get; set; }
    }

    public class Message
    {
        public List<object> MetaData = new List<object>();

        public Message()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }
        public object Content { get; set; }

        public IEnumerable<T> Get<T>()
        {
            return MetaData.OfType<T>();
        }
    }

    public class Message<T> : Message
    {
        public T Content
        {
            get => (T) base.Content;
            set => base.Content = value;
        }
    }
}