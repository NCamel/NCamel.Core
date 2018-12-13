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
                .FromPoller<string>(TimeSpan.FromMinutes(1))
                .To(ctx.FolderMonitorEndpoint(folder: @"c:\temp", deleteFile: true, recursive: false))
                .To(new ConsoleWritelineEndpoint<string>(ctx));

            Console.ReadKey();
        }
    }
}