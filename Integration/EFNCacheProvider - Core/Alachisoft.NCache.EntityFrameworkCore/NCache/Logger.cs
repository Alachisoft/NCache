// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
