using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCamel.Core
{
    public interface IProducer<T>
    {

    }

    public class Context
    {
        public Action ExceptionHandling;
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        List<Task> tasks = new List<Task>();
        public Context()
        {
            Logger.Info("Starting ctx");
        }

        public void Register(Action s)
        {
            tasks.Add(Task.Run(() => s).ContinueWith(Stop));

        }

        private void Stop(Task<Action> obj)
        {
            Logger.Info("Stopping");
        }

        public void RegisterRecurring(TimeSpan minimumDelay, Action f)
        {
            tasks.Add(Task.Run(()=>new Throtler().Execute(minimumDelay, CancellationTokenSource.Token, f)));
        }

    }

    public class Throtler
    {
        public void Execute(TimeSpan minimumDelay, CancellationToken cancellationToken, Action f)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;

                f();

                TimeSpan duration = now - DateTime.Now;

                if (duration < minimumDelay)
                    cancellationToken.WaitHandle.WaitOne(minimumDelay - duration);
            }
        }

    }

    public class Exchange
    {
        public Context Ctx { get; }
        public DateTime StartTime { get; } = DateTime.Now;
        public Exception Exception { get; set; }

        public bool IsFaulted => Exception != null;

        public Message Message { get; set; }

        public Action<Exchange> OnCompleteHandler { get; set; }

        public Exchange(Context ctx, Action<Exchange> onComplete)
        {
            Ctx = ctx;
            OnCompleteHandler = onComplete;
        }
    }

    public class Message
    {
        public Guid Id { get; }
        public Dictionary<string, object> MetaData = new Dictionary<string, object>();
        public object Content { get; set; }

        public Message()
        {
            Id = Guid.NewGuid();
        }
    }

    public class Message<T> : Message
    {
        public T Content
        {
            get { return (T) base.Content; }
            set { base.Content = value; }
        }
    }
}