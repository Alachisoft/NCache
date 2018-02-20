using Microsoft.Extensions.Logging;
using System;

namespace Alachisoft.NCache.EntityFrameworkCore.NCache
{
    internal class Logger
    {
        private static EventId eventId = new EventId(1);

        internal static void Log(string message, LogLevel logLevel)
        {
            if (QueryCacheManager.Loggers != default(ILogger[]))
            {
                foreach (ILogger logger in QueryCacheManager.Loggers)
                {
                    logger.Log(logLevel, eventId, message, null, LogFormatter);
                }
            }
        }

        internal static void Log(Exception e, LogLevel logLevel)
        {
            if (QueryCacheManager.Loggers != default(ILogger[]))
            {
                foreach (ILogger logger in QueryCacheManager.Loggers)
                {
                    logger.Log(logLevel, eventId, e, null, LogFormatter);
                }
            }
        }

        private static string LogFormatter<TState>(TState state, Exception exception)
        {
            return "NCache EF Core Provider\t:\t" + (exception != null ? exception.ToString() : (state != null ? state.ToString() : ""));
        }
    }
}
