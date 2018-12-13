using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCamel.Core
{
    public interface IProducer
    {
        Context Ctx { get; set; }
        Route Route { get; set; }
        void Execute();
    }

    public interface IProducer<TOut> : IProducer
    {
        new Route<TOut> Route { get; set; }
    }

    public interface IStep
    {
        void Execute(Exchange e);
        void OnComplete(Exchange e);
    }

    public interface Step<TIn,TOut> : IStep
    {
    }

    public class StepAdaptor<TIn,TOut>: Step<TIn,TOut>
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
        public string Name { get; set; }
        internal readonly List<IStep> Steps = new List<IStep>();

        public Route()
        {
        }

        internal Route(string name, List<IStep> steps)
        {
            Name = name;
            Steps = steps;
        }

        public Route(string name)
        {
            Name = name;
        }
    }

    public class Route<T> : Route
    {
        internal Route()
        {
            }

        internal Route(string name, List<IStep> steps) : base(name, steps)
        {}

        public Route<TResult> To<TInput, TResult>(Step<TInput, TResult> a)
        {
            Steps.Add(a);
            return new Route<TResult>(Name, Steps);
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
            return new Route(name);
        }

        public void Register(IProducer s)
        {
            tasks.Add(
                Task.Run(()=>s.Execute(), CancellationTokenSource.Token)
                .ContinueWith(Stop));
        }

        public void StartAsync(Exchange e)
        {
            // dont add to task as a route is short lived---
            Task.Run(() => e.Execute(), CancellationTokenSource.Token).ContinueWith(Stop);
        }

        public void Start(Exchange e)
        {
            e.Execute();
        }

        public Exchange CreateExchange(Route route, Message message = null, Action<Exchange> onComplete = null)
        {
            return new Exchange(this, route, message, onComplete);
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

        public Exchange(Context ctx, Route route, Message message, Action<Exchange> onComplete)
        {
            Ctx = ctx;
            Route = route;
            Message = message;

            if(onComplete!=null)
                OnCompleteActions.Push(onComplete);
        }

        public Context Ctx { get; }
        public DateTime StartTime { get; } = DateTime.Now;
        public Exception Exception { get; set; }

        public bool IsFaulted => Exception != null;

        public Message Message { get; set; }

        public Route Route;
        public Stack<Action<Exchange>> OnCompleteActions = new Stack<Action<Exchange>>();

        internal void Execute()
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