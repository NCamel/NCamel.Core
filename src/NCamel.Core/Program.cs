using System;
using System.Threading;
using NCamel.Core.FileEndpoint;

namespace NCamel.Core
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var ctx = new Context();

            ctx.Register(() =>
            {
                new Throtler().Execute(
                    TimeSpan.FromSeconds(1),
                    ctx.CancellationTokenSource.Token,
                    () => new FolderMonitorEndpoint(ctx).Folder(@"c:\temp").Execute());
            });

            Thread.Sleep(4000);
        }
    }
}