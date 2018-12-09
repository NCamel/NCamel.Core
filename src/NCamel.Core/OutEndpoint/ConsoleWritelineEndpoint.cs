using System;
using System.Linq;

namespace NCamel.Core
{
    public class ConsoleWritelineEndpoint
    {
        private readonly Context ctx;

        public ConsoleWritelineEndpoint(Context ctx)
        {
            this.ctx = ctx;
        }

        public void Execute(Exchange e)
        {
            Console.WriteLine("ConsoleWritelineEndpoint");
            Console.WriteLine($"id:{e.Message.Id}");
            Console.WriteLine("Headers");
            e.Message.MetaData.ToList().ForEach(x => Console.WriteLine(x.Key + "=" + x.Value.ToString()));
            Console.WriteLine($"content:{e.Message.Content}");
        }
    }
}