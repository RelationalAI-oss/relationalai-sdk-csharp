using System;

namespace RelationalAI.Helpers
{
    public class ConsoleLogger : ILogger
    {
        public void Info(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Warning(string msg)
        {
            Console.Error.WriteLine($"[WARN] {msg}");
        }

        public void Error(string msg, Exception exception = null)
        {
            Console.Error.WriteLine($"[ERROR] {msg}\n{exception?.Message}\n{exception?.StackTrace}");
        }
    }
}
