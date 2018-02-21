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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Alachisoft.NCache.EntityFrameworkCore.NCache
{
    class EFLoggerFactory : ILoggerFactory
    {
        private static volatile EFLoggerFactory instance;
        private static object synchronizationLock = new object();

        private Microsoft.Extensions.Logging.ILogger logger;

        internal static EFLoggerFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (synchronizationLock)
                    {
                        if (instance == null)
                        {
                            instance = new EFLoggerFactory();
                        }
                    }
                }
                return instance;
            }
        }

        private EFLoggerFactory()
        {
            logger = new EFLogger();
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => logger;

        public void AddProvider(ILoggerProvider provider) { }

        public void Dispose() { }
    }

    class EFLogger : Microsoft.Extensions.Logging.ILogger
    {
        private string path;
        private bool isConfigured;
        private LogLevel logLevel;
        private NCacheLogger nCacheLogger;

        protected internal EFLogger()
        {
            path = AppUtil.LogDir;
            isConfigured = false;
            logLevel = LogLevel.None;
        }

        protected internal bool IsEnabled(LogLevel logLevel) => this.logLevel >= logLevel;

        protected internal void Configure(LogLevel logLevel, string path)
        {
            if (!isConfigured)
            {
                this.logLevel = logLevel;

                if (path != null)
                {
                    if (File.Exists(path))
                    {
                        throw new Exception("The specified location for logging is not a directory but a path to a file.");
                    }
                    if (!Directory.Exists(path))
                    {
                        throw new Exception("The directory specified for logging does not exist.");
                    }
                    this.path = path;
                }
#if JAVA
                this.path = Path.Combine(this.path, @"log");
#else
                this.path = Path.Combine(this.path, @"log-files");
#endif
                this.path = Path.Combine(this.path, @"EFCoreLogs");

                if (!Directory.Exists(this.path))
                {
                    Directory.CreateDirectory(this.path);
                }
                IDictionary properties = new Dictionary<string, string>
                {
                    ["log-path"] = this.path
                };
                nCacheLogger = new NCacheLogger();
                nCacheLogger.Initialize(properties, NCacheConfiguration.CacheId, "EFCoreLogs");

                string logLevelLog4Net = default(string);

                switch (logLevel)
                {
                    case LogLevel.Trace:
                        logLevelLog4Net = LoggerLevel.All;
                        break;
                    case LogLevel.Debug:
                        logLevelLog4Net = LoggerLevel.Debug;
                        break;
                    case LogLevel.Information:
                        logLevelLog4Net = LoggerLevel.Info;
                        break;
                    case LogLevel.Warning:
                        logLevelLog4Net = LoggerLevel.Warn;
                        break;
                    case LogLevel.Error:
                        logLevelLog4Net = LoggerLevel.Error;
                        break;
                    case LogLevel.Critical:
                        logLevelLog4Net = LoggerLevel.CriticalInfo;
                        break;
                    case LogLevel.None:
                        logLevelLog4Net = LoggerLevel.Off;
                        break;
                }

                if (string.IsNullOrEmpty(logLevelLog4Net))
                {
                    logLevelLog4Net = LoggerLevel.Off;
                }
                nCacheLogger.SetLevel(logLevelLog4Net);

                isConfigured = true;
            }
        }

        /* ********************************************************************************************************************** */
        /* *                                    INTERFACE IMPLEMENTATION STARTS HERE                                            * */
        /* ********************************************************************************************************************** */

        bool Microsoft.Extensions.Logging.ILogger.IsEnabled(LogLevel logLevel) => IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (isConfigured && IsEnabled(logLevel))
            {
                GetLoggingMethod(logLevel).Invoke(nCacheLogger, new object[] { formatter(state, exception) });
            }
        }

        /* ********************************************************************************************************************** */
        /* *                                      INTERFACE IMPLEMENTATION ENDS HERE                                            * */
        /* ********************************************************************************************************************** */

        private MethodInfo GetLoggingMethod(LogLevel logLevel)
        {
            string methodName = default(string);

            switch (logLevel)
            {
                case LogLevel.Trace:
                    methodName = "Info";
                    break;
                case LogLevel.Debug:
                    methodName = "Debug";
                    break;
                case LogLevel.Information:
                    methodName = "Info";
                    break;
                case LogLevel.Warning:
                    methodName = "Warn";
                    break;
                case LogLevel.Error:
                    methodName = "Error";
                    break;
                case LogLevel.Critical:
                    methodName = "Fatal";
                    break;
                case LogLevel.None:
                    methodName = "Debug";
                    break;
            }
            if (string.IsNullOrEmpty(methodName))
            {
                methodName = "Debug";
            }
            return nCacheLogger.GetType().GetMethod(methodName, new Type[] { typeof(string) });
        }
    }
}
