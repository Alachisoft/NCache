// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using Microsoft.Extensions.Logging;

namespace Alachisoft.NCache.Samples
{
    class LoggerProvider : ILoggerProvider
    {
        /*
         * The logger provider provides loggers that are Microsoft.Extensions.Logging.ILogger 
         * in signature. For information on how to work with it, go to the following link,
         * https://docs.microsoft.com/en-us/ef/core/miscellaneous/logging
         */
        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName.Equals("NCacheLogger"))
            {
                return new NCacheLogger();
            }
            return new Logger();
        }

        public void Dispose() { }
    }

    // Making a logger that will be responsible for logging
    // entity framework core's messages
    class Logger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;    // Log level set to information and above
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine("EF Log : " + formatter(state, exception));
        }
    }

    // Making a logger that will be responsible for logging
    // NCache entity framework core provider's messages
    class NCacheLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Debug;      // Log level set to debug only
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine("NCache Log : " + formatter(state, exception));
        }
    }
}
