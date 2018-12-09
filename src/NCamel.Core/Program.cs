using System;
using NCamel.Core.FileEndpoint;

namespace NCamel.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            var  ctx =new Context();

            ctx.Register(() =>
            {
                new Throtler().Execute(
                    TimeSpan.FromSeconds(1), 
                    ctx.CancellationTokenSource.Token,  
                    () => new FolderMonitorEndpoint(ctx).Folder(@"c:\temp").Execute());
            });
        }
    }
}

