using System;
using NCamel.Core.FileEndpoint;

namespace NCamel.Core
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ctx = new Context();

            ctx
                .FromPoller(TimeSpan.FromMinutes(1))
                .To(() => ctx.FolderMonitorEndpointBuilder()
                    .Folder(@"c:\temp")
                    .DeleteFile()
                    .Recursive(false)
                    .Build())
                .To(new ConsoleWritelineEndpoint(ctx));

            Console.ReadKey();
        }
    }
}