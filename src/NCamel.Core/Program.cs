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
                .To(() =>
                    new FolderMonitorEndpoint(ctx)
                        .Folder(@"c:\temp")
                        .DeleteFile()
                        .To(new Route("invoices", ctx)));

            Console.ReadKey();
        }
    }
}