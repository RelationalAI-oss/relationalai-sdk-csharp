using System;

namespace RelationalAI.Helpers
{
    public interface ILogger
    {
        void Info(string msg);

        void Warning(string msg);

        void Error(string msg, Exception exception = null);
    }
}
