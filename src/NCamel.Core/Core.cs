using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCamel.Core
{
    public interface IProducer
    {
        void Execute();
    }

    public interface Step
    {
        void Execute(Exchange e);
        void OnComplete(Exchange e);
    }

    public class StepAdaptor : Step
    {
        private readonly Action<Exchange> execute;
        private readonly Action<Exchange> onComplete;

        public StepAdaptor(Action<Exchange> execute, Action<Exchange> onComplete)
        {
            this.execute = execute;
            this.onComplete = onComplete;
        }
        public void Execute(Exchange e)
        {
            execute(e);
        }

        public void OnComplete(Exchange e)
        {
            onComplete(e);
        }
    }

    public class Route
    {
        public readonly string Name;
        private readonly Context ctx;
        public readonly List<Step> Steps;

        public Route(string name, Context ctx)
        {
            Name = name;
            this.ctx = ctx;
            Steps=new List<Step>();
        }

        public Route From(Step a)
        {
            Steps.Add(a);
            return this;
        }

        public Route To(Step a)
        {
            Steps.Add(a);
            return this;
        }
    }

    public class Context
    {
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public Action ExceptionHandling;

        private readonly List<Task> tasks = new List<Task>();

        public Context()
        {
            Logger.Info("Starting ctx");
        }

        public Route Route(string name)
        {
            return new Route(name, this);
        }

        public void Register(IProducer s)
        {
            tasks.Add(Task.Run(()=>s.Execute(), CancellationTokenSource.Token).ContinueWith(Stop));
        }

        public void Start(Exchange e)
        {
            // dont add to task as a route is short lived---
            Task.Run(()=>e.Execute(), CancellationTokenSource.Token).ContinueWith(Stop);
        }

        private void Stop(Task obj)
        {
        }
    }

    public class Exchange
    {
        public Exchange(Context ctx, Route route)
        {
            Ctx = ctx;
            Route = route;
        }

        public Context Ctx { get; }
        public DateTime StartTime { get; } = DateTime.Now;
        public Exception Exception { get; set; }

        public bool IsFaulted => Exception != null;

        public Message Message { get; set; }

        public Route Route;
        public Stack<Action<Exchange>> OnCompleteActions = new Stack<Action<Exchange>>();

        public void Execute()
        {
            foreach (var step in Route.Steps)
                step.Execute(this);

            while (OnCompleteActions.Any())
                OnCompleteActions.Pop()(this);

            //Logger.Info($"Ending: '{Route.Name}'. Duration: {(DateTime.Now-StartTime).TotalMilliseconds}");
        }
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