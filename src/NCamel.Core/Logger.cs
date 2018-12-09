using System;

namespace NCamel.Core
{
    public static class Logger
    {
        public static Action<string> Info = s => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} INFO {s}");
        public static Action<string> Warn= s => Console.WriteLine($"{DateTime.Now.ToLongTimeString()} WARN {s}");
    }
}