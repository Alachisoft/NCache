//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.IO;
using log4net.Appender;

namespace Alachisoft.NCache.Common.Util
{
    public static class NCacheLog 
    {
        public const int bufferDefaultSize = 1;
        static DateTime _timeFired = DateTime.Now;
        static object syncLock = new object();
        static private string s_config = "";
        private const string DIRNAME = @"bin\service";

        static NCacheLog()
        {

#if !NETCORE && !NETCOREAPP2_0
            string serverconfig ="Alachisoft.NCache.Service.exe.config";
#else
            string serverconfig = "Alachisoft.NCache.Daemon.dll.config";
#endif
        }

        public static void SetLevel(string cacheName, Level level)
        {
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                lock (NCacheLog.syncLock)
                {
                    log4net.Core.Level lvl;
                    switch (level.ToString().ToLower())
                    {
                        case "all":
                            lvl = log4net.Core.Level.All;
                            break;
                        case "error":
                            lvl = log4net.Core.Level.Error;
                            break;
                        case "fatal":
                            lvl = log4net.Core.Level.Fatal;
                            break;
                        case "info":
                            lvl = log4net.Core.Level.Info;
                            break;
                        case "debug":
                            lvl = log4net.Core.Level.Debug;
                            break;
                        case "warn":
                            lvl = log4net.Core.Level.Warn;
                            break;
                        case "off":
                            lvl = log4net.Core.Level.Off;
                            break;
                        default:
                            lvl = log4net.Core.Level.All;
                            break;

                    }


                    //If the logger doesnot exist it will create one else fetches one
                    log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                    //adds the logger as a seperate hierchy, not dependant on any other logger
                    log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;

                    //Applies the logger threshold level
                    l.Level = l.Hierarchy.LevelMap[level.ToString()];

                    IAppender[] appenderCol = log.Logger.Repository.GetAppenders();

                    for (int i = 0; i < appenderCol.Length; i++)
                    {
                        IAppender appender = appenderCol[i];

                        if (appender != null)
                        {

                            if (appender is BufferingForwardingAppender)
                            {
                                ((BufferingForwardingAppender)appender).Threshold = lvl;
                            }

                            if (appender is RollingFileAppender)
                            {
                                ((RollingFileAppender)appender).Threshold = lvl;
                            }
                        }
                    }
                }
            }
        }

        static void SetBufferSize(int bufferSize)
        {
            lock (NCacheLog.syncLock)
            {
#if !NETCORE
                foreach (log4net.Appender.IAppender appender in log4net.LogManager.GetRepository().GetAppenders())
#elif NETCORE
                    //TODO: ALACHISOFT (Adding Repository directly here for now)
                    foreach (log4net.Appender.IAppender appender in log4net.LogManager.GetRepository(Log4net.LogRepository.Name).GetAppenders())
#endif
                {
                    BufferingAppenderSkeleton buffered = appender as BufferingAppenderSkeleton;
                    if (buffered is BufferingForwardingAppender)
                    {
                        buffered.BufferSize = bufferSize;
                        buffered.ActivateOptions();
                    }
                }
            }
        }

#region Main Loggers
        public static void Error(string cacheName, string message)
        {
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                if (message.Contains(Environment.NewLine))
                    message = message + "\r\n";
                log.Error(message);
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
        }

        public static void Fatal(string cacheName, string message)
        {
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                if (message.Contains(Environment.NewLine))
                    message = message + "\r\n";
                log.Fatal(message);
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
        }

        public static void CriticalInfo(string cacheName, string message)
        {
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                if (message.Contains(Environment.NewLine))
                    message = message + "\r\n";
                log.Logger.Log(null, Log4net.criticalInfo, message, null);
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
        }

        public static void Info(string cacheName, string message)
        {
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                if (message.Contains(Environment.NewLine))
                    message = message + "\r\n";
                log.Info(message);
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
        }

        public static void Debug(string cacheName, string message)
        {
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                if (message.Contains(Environment.NewLine))
                    message = message + "\r\n";
                log.Debug(message);
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
        }

        public static void Warn(string cacheName, string message)
        {
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                if (message.Contains(Environment.NewLine))
                    message = message + "\r\n";
                log.Warn(message);
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
        }
#endregion

#region IfEnabled

        public static bool IsInfoEnabled(string cacheName)
        {
#region isEnalbedLogic
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return false;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                return log.IsInfoEnabled;
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
            return false;
#endregion

            return true;

        }

        public static bool IsErrorEnabled(string cacheName)
        {
#region isEnabledLogic
           
#endregion

            return true;
        }

        public static bool IsWarnEnabled(string cacheName)
        {
#region isEnabledLogic
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return false;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                return log.IsInfoEnabled;
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }

            return false;
#endregion

            return true;
        }

        public static bool IsDebugEnabled(string cacheName)
        {
#region isEnabledLogic
            if (cacheName == null || cacheName == string.Empty)
            {
                NCacheLog.LogLoggingError("Cache Name is null");
                return false;
            }
            string loggerName = LoggingInformation.GetLoggerName(cacheName);
            if (loggerName != null && loggerName.Length > 0)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                return log.IsInfoEnabled;
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }

            return false;
#endregion

            return true;
        }

        public static bool IsFatalEnabled(string cacheName)
        {
#region isEnabledLogic
           
#endregion

            return true;
        }

#endregion

#region Enum Overloads

        public static void SetLevel(LoggerNames loggerEnum, Level level)
        {
            string loggerName = LoggingInformation.GetLoggerName(LoggingInformation.GetStaticLoggerName(loggerEnum.ToString()));
            if (loggerName != null && loggerName.Length > 0)
            {
                lock (NCacheLog.syncLock)
                {
                    log4net.Core.Level lvl;
                    switch (level.ToString().ToLower())
                    {
                        case "all":
                            lvl = log4net.Core.Level.All;
                            break;
                        case "error":
                            lvl = log4net.Core.Level.Error;
                            break;
                        case "fatal":
                            lvl = log4net.Core.Level.Fatal;
                            break;
                        case "info":
                            lvl = log4net.Core.Level.Info;
                            break;
                        case "debug":
                            lvl = log4net.Core.Level.Debug;
                            break;
                        case "warn":
                            lvl = log4net.Core.Level.Warn;
                            break;
                        case "off":
                            lvl = log4net.Core.Level.Off;
                            break;
                        default:
                            lvl = log4net.Core.Level.All;
                            break;

                    }


                    //If the logger doesnot exist it will create one else fetches one
                    log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                    //adds the logger as a seperate hierchy, not dependant on any other logger
                    log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;

                    //Applies the logger threshold level
                    l.Level = l.Hierarchy.LevelMap[level.ToString()];

                    IAppender[] appenderCol = log.Logger.Repository.GetAppenders();

                    for (int i = 0; i < appenderCol.Length; i++)
                    {
                        IAppender appender = appenderCol[i];

                        if (appender != null)
                        {

                            if (appender is BufferingForwardingAppender)
                            {
                                ((BufferingForwardingAppender)appender).Threshold = lvl;
                            }

                            if (appender is RollingFileAppender)
                            {
                                ((RollingFileAppender)appender).Threshold = lvl;
                            }
                        }
                    }
                }
            }
            else
            {
                NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
            }
        }

#region  Enabled Enum Methods
        public static bool IsInfoEnabled(NCacheLog.LoggerNames loggerEnum)
        {
#region isEnabledLogic
            string temp = LoggingInformation.GetStaticLoggerName(loggerEnum.ToString());
            if (temp != null && temp.Length > 0)
            {
                string loggerName = LoggingInformation.GetLoggerName(temp);
                if (loggerName != null && loggerName.Length > 0)
                {
                    log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                    return log.IsInfoEnabled;
                }
                else
                {
                    NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
                }
            }
            return false;
#endregion

        }

        public static bool IsErrorEnabled(NCacheLog.LoggerNames loggerEnum)
        {
#region isEnabledLogic
           
#endregion

            return true;
        }

        public static bool IsWarnEnabled(NCacheLog.LoggerNames loggerEnum)
        {
#region isEnabledLogic
          
#endregion

            return true;
        }

        public static bool IsDebugEnabled(NCacheLog.LoggerNames loggerEnum)
        {
#region isEnabledLogic
            string temp = LoggingInformation.GetStaticLoggerName(loggerEnum.ToString());
            if (temp != null && temp.Length > 0)
            {
                string loggerName = LoggingInformation.GetLoggerName(temp);
                if (loggerName != null && loggerName.Length > 0)
                {
                    log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                    return log.IsDebugEnabled;
                }
                else
                {
                    NCacheLog.LogLoggingError("loggerName != null && loggerName.Length > 0");
                }
            }
            return false;
#endregion
        }

        public static bool IsFatalEnabled(NCacheLog.LoggerNames loggerEnum)
        {
#region isEnabledLogic
          
#endregion

            return true;
        }
#endregion

        public static void Error(NCacheLog.LoggerNames loggerName, string message)
        {
            string name = loggerName.ToString();
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Error(name, message);
        }

        public static void Fatal(NCacheLog.LoggerNames loggerName, string message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Fatal(name, message);
        }

        public static void CriticalInfo(NCacheLog.LoggerNames loggerName, string message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            CriticalInfo(name, message);
        }


        public static void Info(NCacheLog.LoggerNames loggerName, string message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Info(name, message);
        }

        public static void Debug(NCacheLog.LoggerNames loggerName, string message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Debug(name, message);
        }

        public static void Warn(NCacheLog.LoggerNames loggerName, string message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Warn(name, message);
        }


        //------------------------------------------------------------------------------------------------
        public static void Error(NCacheLog.LoggerNames loggerName, String module, String message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Error(name, module, message);
        }

        public static void Fatal(NCacheLog.LoggerNames loggerName, String module, String message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Fatal(name, module, message);
        }

        public static void CriticalInfo(NCacheLog.LoggerNames loggerName, String module, String message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            CriticalInfo(name, module, message);
        }

        public static void Info(NCacheLog.LoggerNames loggerName, String module, String message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Info(name, module, message);
        }

        public static void Debug(NCacheLog.LoggerNames loggerName, String module, String message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Debug(name, module, message);
        }

        public static void Warn(NCacheLog.LoggerNames loggerName, String module, String message)
        {
            string name = loggerName.ToString(); ;
            if (loggerName != LoggerNames.ClientLogs)
                name = LoggingInformation.GetStaticLoggerName(loggerName.ToString());
            Warn(name, module, message);
        }

#endregion

#region Overloaded Logging Methods

        public static void Error(string cacheName, String module, String message)
        {
            int space1 = 8;
            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Error(cacheName, line);

        }

        public static void Fatal(string cacheName, String module, String message)
        {
            int space1 = 8;
            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Fatal(cacheName, line);
        }

        public static void CriticalInfo(string cacheName, String module, String message)
        {
            int space1 = 8;
            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            CriticalInfo(cacheName, line);
        }

        public static void Info(string cacheName, String module, String message)
        {
            int space1 = 8;
            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Info(cacheName, line);
        }

        public static void Debug(string cacheName, String module, String message)
        {
            int space1 = 8;
            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Debug(cacheName, line);
        }

        public static void Warn(string cacheName, String module, String message)
        {
            int space1 = 8;
            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Warn(cacheName, line);
        }

#endregion

        public enum Level
        {
            ERROR,
            INFO,
            WARNING,
            DEBUG,
            ALL,
            OFF
        }

        public enum LoggerNames
        {
            OutputCache,
            HttpModule,
            SessionStoreProvider,
            Licence,
            SocketServerLogs,
            ClientLogs

        }
        
        public static void OnChange()
        {
            bool[] logLevel = new bool[2];
            int bufferSize = bufferDefaultSize;
            lock (syncLock)
            {
                if (_timeFired != null)
                {
                    if (DateTime.Now.Subtract(_timeFired).TotalMilliseconds < 50) return;
                }
                _timeFired = DateTime.Now;


                System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                logLevel = ReadConfig(out bufferSize);
            }

            string loggerName = LoggingInformation.GetStaticLoggerName(LoggerNames.SocketServerLogs.ToString());

            if (loggerName != null)
            {
                log4net.ILog log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, loggerName);
                log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;
               
                BufferingAppenderSkeleton buffered = (BufferingAppenderSkeleton)l.GetAppender("BufferingForwardingAppender" + loggerName);
                if (buffered is BufferingForwardingAppender)
                {
                    ((BufferingForwardingAppender)buffered).Flush();
                }
               
            }

            //if not already initialized
            Log4net.Initialize(LoggerNames.SocketServerLogs);

            NCacheLog.SetLevel(LoggerNames.SocketServerLogs.ToString(), logLevel[1] == true ? NCacheLog.Level.ALL : logLevel[0] == true ? NCacheLog.Level.INFO : NCacheLog.Level.OFF);

            if (bufferSize < 1)
                bufferSize = bufferDefaultSize;

            NCacheLog.SetBufferSize(bufferSize);
        }
        
        public static bool[] ReadConfig(out int bufferAppender)
        {
            try
            {
                string EnableLogs = System.Configuration.ConfigurationManager.AppSettings["EnableLogs"];
                string EnableDetailedLogs = System.Configuration.ConfigurationManager.AppSettings["EnableDetailedLogs"];
                string BufferSize = System.Configuration.ConfigurationManager.AppSettings["BufferSize"];


                try
                {
                    if (BufferSize != null)
                    {
                        bufferAppender = Convert.ToInt32(BufferSize);
                    }
                    else
                        bufferAppender = bufferDefaultSize;
                }
                catch (Exception)
                {
                    bufferAppender = bufferDefaultSize;
                }

                if (EnableDetailedLogs == null && EnableLogs == null)
                {
                    return new bool[2] { false, false };
                }
                else if (EnableDetailedLogs != null && EnableLogs == null)
                {
                    return new bool[2] { false, Convert.ToBoolean(EnableDetailedLogs) };
                }
                else if (EnableDetailedLogs == null && EnableLogs != null)
                {
                    return new bool[2] { Convert.ToBoolean(EnableLogs), false };
                }
                else
                {
                    return new bool[2] { Convert.ToBoolean(EnableLogs), Convert.ToBoolean(EnableDetailedLogs) };
                }
            }
            catch (Exception ex)
            {
                bufferAppender = bufferDefaultSize;
                return new bool[2] { false, false };
                NCacheLog.LogLoggingError(ex.Message);
            }

        }

        public static void OnClientChange(object _o, FileSystemEventArgs _a)
        {
            bool[] logLevel = new bool[2];
            int bufferSize = bufferDefaultSize;
            lock (syncLock)
            {
                if (_timeFired != null)
                {
                    if (DateTime.Now.Subtract(_timeFired).TotalMilliseconds < 50) return;
                }
                _timeFired = DateTime.Now;


                System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                logLevel = ReadClientConfig(out bufferSize);
            }

            //if not already initialized
           
            foreach (string loggerName in LoggingInformation.cacheLogger.Values)
            {
                NCacheLog.SetLevel(loggerName, logLevel[1] == true ? NCacheLog.Level.ALL : logLevel[0] == true ? NCacheLog.Level.INFO : NCacheLog.Level.OFF);
            }

            if (bufferSize < 1)
                bufferSize = bufferDefaultSize;

            NCacheLog.SetBufferSize(bufferSize);

        }

        public static bool[] ReadClientConfig(out int bufferAppender)
        {
            try
            {
                string EnableLogs = System.Configuration.ConfigurationManager.AppSettings["EnableNCWebLogs"];
                string EnableDetailedLogs = System.Configuration.ConfigurationManager.AppSettings["EnableDetailedNCWebLogs"];
                string BufferSize = System.Configuration.ConfigurationManager.AppSettings["BufferSize"];


                try
                {
                    if (BufferSize != null)
                    {
                        bufferAppender = Convert.ToInt32(BufferSize);
                    }
                    else
                        bufferAppender = bufferDefaultSize;
                }
                catch (Exception)
                {
                    bufferAppender = bufferDefaultSize;
                }

                if (EnableDetailedLogs == null && EnableLogs == null)
                {
                    return new bool[2] { false, false };
                }
                else if (EnableDetailedLogs != null && EnableLogs == null)
                {
                    return new bool[2] { false, Convert.ToBoolean(EnableDetailedLogs) };
                }
                else if (EnableDetailedLogs == null && EnableLogs != null)
                {
                    return new bool[2] { Convert.ToBoolean(EnableLogs), false };
                }
                else
                {
                    return new bool[2] { Convert.ToBoolean(EnableLogs), Convert.ToBoolean(EnableDetailedLogs) };
                }
            }
            catch (Exception ex)
            {
                bufferAppender = bufferDefaultSize;
                return new bool[2] { false, false };
                NCacheLog.LogLoggingError(ex.Message);
            }

        }


#region IDisposable Members

        public static void Flush()
        {
            //bug id- 1431 Problem was occuring because the Appender enumeration was modifying during iterations
#if !NETCORE
            IAppender[] logAppenders = log4net.LogManager.GetRepository().GetAppenders();
#elif NETCORE
            //TODO: ALACHISOFT (Direct edit to code for now. It should be removed later on)
            IAppender[] logAppenders = log4net.LogManager.GetRepository(Log4net.LogRepository.Name).GetAppenders();
#endif

            foreach (log4net.Appender.IAppender appender in logAppenders)
            {
                if (appender != null)
                {
                    BufferingAppenderSkeleton buffered = appender as BufferingAppenderSkeleton;
                    if (buffered is BufferingForwardingAppender)
                    {
                        ((BufferingForwardingAppender)buffered).Flush();
                    }
                }
            }
        }

#endregion

        public static void LogLoggingError(string msg)
        {
           
        }


    }
}