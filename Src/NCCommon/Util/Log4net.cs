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
using System.Collections;

using System.IO;
using System.Reflection;
using log4net.Appender;
using System.Text;

namespace Alachisoft.NCache.Common.Util
{
    /// <summary>
    /// Summary description for Log.
    /// </summary>
    //TODO: ALACHISOFT (For now I have changed code here directly. Do something so that we don't have to)
    public sealed class Log4net
    {
        static string _cacheserver="NCache";
        private const string DIRNAME = @"log-files";
        private static byte[] log4netXML = Encoding.ASCII.GetBytes("<?xml version=\"1.0\"?> <configuration>  <configSections>   <section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler, log4net, Version=1.2.10.0, Culture = neutral, PublicKeyToken=1b44e1d426115821 \"/> </configSections>   <log4net> </log4net> </configuration>");
        public static log4net.Core.Level criticalInfo = new log4net.Core.Level(5000000, "CRIT", "INFO");
        public static log4net.Repository.ILoggerRepository LogRepository = log4net.LogManager.GetRepository(Assembly.GetCallingAssembly());
        static private string s_configDir = "";
        static private string path = "";
        static object lockObj = new Object();

        /// <summary>
        /// Scans the registry and locates the configuration file.
        /// </summary>
        static Log4net()
        {
            s_configDir = AppUtil.LogDir;

            try
            {
                s_configDir = System.IO.Path.Combine(s_configDir, DIRNAME);
            }
            catch { }

            if (s_configDir == null && s_configDir.Length > 0)
            {
                s_configDir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                s_configDir = System.IO.Path.Combine(s_configDir, DIRNAME);
            }


        }

        internal static string GetLogPath()
        {
            if (path.Length < 1)
            {
                path = s_configDir;
            }
            try
            {
                log4net.LogManager.GetRepository(LogRepository.Name).LevelMap.Add(criticalInfo);

                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                AppUtil.LogEvent(_cacheserver, e.ToString(), System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
            }
            return path;
        }

        /// <summary>
        /// Start the cache logging functionality.
        /// </summary>
        public static string Initialize(IDictionary properties, string partitionID, string cacheName)
        {
            return Initialize(properties, partitionID, cacheName, false, false);
        }

        /// <summary>
        /// Start the cache logging functionality.
        /// </summary>
        public static string Initialize(IDictionary properties, string partitionID, string cacheName, bool isStartedAsMirror, bool inproc)
        {

            lock (lockObj)
            {
                MemoryStream logStream = new MemoryStream(log4netXML);
                log4net.Config.XmlConfigurator.Configure(LogRepository, logStream);

                string logger_name = "";

                try
                {
                    logger_name = cacheName;
                    if (partitionID != null && partitionID.Length > 0)
                        logger_name += "-" + partitionID;

                    if (isStartedAsMirror)
                        logger_name += "-" + "replica";

                    if (inproc && !isStartedAsMirror) logger_name += "." + System.Diagnostics.Process.GetCurrentProcess().Id;

                    //Add loggerName to be accessed w.r.t the cache Name
                    if (!LoggingInformation.cacheLogger.Contains(logger_name))
                    {
                        LoggingInformation.cacheLogger.Add(logger_name, logger_name);

                        string LogExceptions = "";

                        if (logger_name == "LogExceptions")
                            LogExceptions = Path.DirectorySeparatorChar+"LogExceptions";

                        string fileName = GetLogPath() + LogExceptions + Path.DirectorySeparatorChar + logger_name + "_" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".txt"; ;

                        AddAppender(logger_name, CreateBufferAppender(logger_name, fileName));

                        if (properties != null)
                        {
                            if (properties.Contains("trace-errors"))
                            {
                                if (Convert.ToBoolean(properties["trace-errors"]))
                                {
                                    SetLevel(logger_name, "ERROR");
                                }
                            }

                            if (properties.Contains("trace-notices"))
                            {
                                if (Convert.ToBoolean(properties["trace-notices"]))
                                {
                                    SetLevel(logger_name, "INFO");
                                }
                            }

                            if (properties.Contains("trace-warnings"))
                                if (Convert.ToBoolean(properties["trace-warnings"]))
                                {
                                    SetLevel(logger_name, "WARN");
                                }

                            if (properties.Contains("trace-debug"))
                                if (Convert.ToBoolean(properties["trace-debug"]))
                                {
                                    SetLevel(logger_name, "ALL");
                                }

                            if (properties.Contains("enabled"))
                            {
                                if (!Convert.ToBoolean(properties["trace-errors"]))
                                {
                                    SetLevel(logger_name, "OFF");
                                }
                            }
                        }
                        else
                        {
                            SetLevel(logger_name, "WARN");
                        }

                        return logger_name;
                    }
                    else
                    {
                        if (properties != null)
                        {
                            if (properties.Contains("trace-errors"))
                            {
                                if (Convert.ToBoolean(properties["trace-errors"]))
                                {
                                    SetLevel(logger_name, "ERROR");
                                }
                            }

                            if (properties.Contains("trace-notices"))
                            {
                                if (Convert.ToBoolean(properties["trace-notices"]))
                                {
                                    SetLevel(logger_name, "INFO");
                                }
                            }

                            if (properties.Contains("trace-warnings"))
                                if (Convert.ToBoolean(properties["trace-warnings"]))
                                {
                                    SetLevel(logger_name, "WARN");
                                }

                            if (properties.Contains("trace-debug"))
                                if (Convert.ToBoolean(properties["trace-debug"]))
                                {
                                    SetLevel(logger_name, "ALL");
                                }

                            if (properties.Contains("enabled"))
                            {
                                if (!Convert.ToBoolean(properties["trace-errors"]))
                                {
                                    SetLevel(logger_name, "OFF");
                                }
                            }
                        }
                        else
                        {
                            SetLevel(logger_name, "WARN");
                        }
                        return logger_name;
                    }

                }
                catch (Exception e)
                {
                    AppUtil.LogEvent(_cacheserver, "Failed to open log. " + e, System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                }
                return logger_name;


            }

        }

        /// <summary>
        /// intitializes Known name based log files (will not log License Logs at service Startup
        /// </summary>
        /// <param name="loggerName">Enum of Known loggerNames</param>
        public static void Initialize(NCacheLog.LoggerNames loggerName)
        {
            if (loggerName != NCacheLog.LoggerNames.Licence)
            {
                Initialize(loggerName, null);
            }
        }


        /// <summary>
        /// intitializes Known name based log files
        /// </summary>
        /// <param name="loggerName">Enum of Known loggerNames</param>
        /// <param name="cacheName">cacheName if Clientcache logging, if not ClientCache, use the other override</param>
        public static void Initialize(NCacheLog.LoggerNames loggerNameEnum, string cacheName)
        {
            lock (lockObj)
            {

                MemoryStream logStream = new MemoryStream(log4netXML);
                log4net.Config.XmlConfigurator.Configure(LogRepository, logStream);

                string logName = loggerNameEnum.ToString();

                string filename = logName;
                if (loggerNameEnum == NCacheLog.LoggerNames.ClientLogs && (cacheName != null && cacheName.Length > 0))
                {
                    filename = filename + "." + cacheName + "." + System.Diagnostics.Process.GetCurrentProcess().Id;

                    logName = cacheName + System.Diagnostics.Process.GetCurrentProcess().Id;
                }
                else
                {
                    if (cacheName != null && cacheName.Length > 0)
                        filename = cacheName;
                }

                //If Logger is already present, can either be a cache or Client
                if (LoggingInformation.GetLoggerName(logName) != null)
                {
                    if (loggerNameEnum == NCacheLog.LoggerNames.ClientLogs)
                        return; // clientLogs alread initiated
                    else
                    {
                        if (LoggingInformation.GetStaticLoggerName(logName) != null)
                            return; // Log already initiated
                        else
                        {
                            logName = logName + DateTime.Now;
                        }
                    }
                }
                else
                {
                    if (loggerNameEnum != NCacheLog.LoggerNames.ClientLogs)
                    {
                        if (LoggingInformation.GetStaticLoggerName(logName) != null)
                            return; // Log already initiated
                        else
                        {
                            logName = logName + DateTime.Now;
                        }
                    }
                }

                filename = filename + "." +
                            Environment.MachineName.ToLower() + "." +
                            DateTime.Now.ToString("dd-MM-yy HH-mm-ss") + @".logs.txt";

                string filepath = "";
                if (!DirectoryUtil.SearchGlobalDirectory("log-files", false, out filepath))
                {
                    try
                    {
                        DirectoryUtil.SearchLocalDirectory("log-files", true, out filepath);

                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Unable to initialize the log file", ex);
                    }
                }

                try
                {
                    filepath = Path.Combine(filepath, loggerNameEnum.ToString());
                    if (!Directory.Exists(filepath)) Directory.CreateDirectory(filepath);

                    filepath = Path.Combine(filepath, filename);

                    LoggingInformation.cacheLogger.Add(logName, logName);
                    if (loggerNameEnum != NCacheLog.LoggerNames.ClientLogs)
                    {
                        LoggingInformation.staticCacheLogger.Add(loggerNameEnum.ToString(), logName);
                    }

                    SetLevel(logName, NCacheLog.Level.OFF.ToString());
                    AddAppender(logName, CreateBufferAppender(logName, filepath));
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Stop the cache logging functionality.
        /// </summary>
        public static void Close(string cacheName)
        {
            lock (lockObj)
            {
                if (cacheName != null)
                    if (cacheName.Length != 0)
                    {
                        string temploggerName = LoggingInformation.GetLoggerName(cacheName);

                        //called at remove cache
                        if (temploggerName != null)
                        {
                            SetLevel(temploggerName, "OFF");

                            if (temploggerName != null)
                            {
                                log4net.ILog log = log4net.LogManager.GetLogger(LogRepository.Name, temploggerName);

                                log4net.Core.IAppenderAttachable closingAppenders = (log4net.Core.IAppenderAttachable)log.Logger;
                                AppenderCollection collection = closingAppenders.Appenders;
                                for (int i = 0; i < collection.Count; i++)
                                {
                                    if (collection[i] is BufferingForwardingAppender)
                                    {
                                        //This FLUSH and close the current appenders along with all of its children appenders
                                        ((BufferingForwardingAppender)collection[i]).Close();
                                    }
                                }

                                RemoveAllAppender(temploggerName);
                                LoggingInformation.cacheLogger.Remove(cacheName);
                            }

                        }
                    }
            }
        }


        /// <summary>
        /// Creates Buffer Appender, Responsible for storing all logging events in memory buffer 
        /// and writing them down when by passing the logging events on to the file appender it holds
        /// when its buffer becomes full
        /// Can also be made a lossy logger which writes only writes down to a file when a specific crieteria/condition is met
        /// </summary>
        /// <param name="cacheName">CacheName used to name the Buffer Appender</param>
        /// <param name="fileName">File name to log into</param>
        /// <returns>Returns the created Appender</returns>
        private static log4net.Appender.IAppender CreateBufferAppender(string cacheName, string fileName)
        {
            log4net.Appender.BufferingForwardingAppender appender = new BufferingForwardingAppender();
            appender.Name = "BufferingForwardingAppender" + cacheName;
            //Pick from config
            int bufferSize = NCacheLog.bufferDefaultSize;
            NCacheLog.ReadConfig(out bufferSize);
            if (bufferSize == NCacheLog.bufferDefaultSize)
                NCacheLog.ReadClientConfig(out bufferSize);

            if (bufferSize < 1)
                bufferSize = NCacheLog.bufferDefaultSize;

            appender.BufferSize = bufferSize;


            //Threshold is maintained by the logger rather than the appenders
            appender.Threshold = log4net.Core.Level.All;

            //Adds the appender to which it will pass on all the logging levels upon filling up the buffer
            appender.AddAppender(CreateRollingFileAppender(cacheName, fileName));

            //necessary to apply the appender property changes
            appender.ActivateOptions();

            return appender;
        }

        /// <summary>
        /// Create File appender, This appender is responsible to write stream of data when invoked, in 
        /// our case, this appender is handeled my the Bufferappender
        /// </summary>
        /// <param name="cacheName">Name of the file appender</param>
        /// <param name="fileName">Filename to which is to write logs</param>
        /// <returns>returns the created appender</returns>
        private static log4net.Appender.IAppender CreateRollingFileAppender(string cacheName, string fileName)
        {
            log4net.Appender.RollingFileAppender appender = new log4net.Appender.RollingFileAppender();
            appender.Name = "RollingFileAppender" + cacheName;
            appender.File = fileName;
            //doesnt matter since all files are created with a new name
            appender.AppendToFile = false;

            appender.RollingStyle = RollingFileAppender.RollingMode.Size;
            appender.MaximumFileSize = "5MB";
            appender.MaxSizeRollBackups = -1;

            //Threshold is maintained by the logger rather than the appenders
            appender.Threshold = log4net.Core.Level.All;

            log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();

            layout.ConversionPattern = "%-27date{ISO8601}" + "\t%-45.42appdomain" + "\t%-35logger" + "\t%-42thread" + "\t%-9level" + "\t%message" + "%newline";

            layout.Header = "TIMESTAMP                  \tAPPDOMAIN                                    \tLOGGERNAME                         \tTHREADNAME                                \tLEVEL    \tMESSAGE\r\n";
            layout.Footer = "END \n";

            layout.ActivateOptions();
            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }

        /// <summary>
        /// Add a desired appender to the logger
        /// </summary>
        /// <param name="loggerName">Name of the logger to which the appender is to be added</param>
        /// <param name="appender">Appender to add to the logger</param>
        private static void AddAppender(string loggerName, log4net.Appender.IAppender appender)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(LogRepository.Name, loggerName);
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;

            l.AddAppender(appender);
        }

        private static void RemoveAllAppender(string loggerName)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(LogRepository.Name, loggerName);
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;

            l.RemoveAllAppenders();
        }



        /// <summary>
        /// Set desire level for a specific logger
        /// </summary>
        /// <param name="loggerName">Name of the logger</param>
        /// <param name="levelName">Name of the desire level</param>
        private static void SetLevel(string loggerName, string levelName)
        {
            log4net.Core.Level lvl;
            switch (levelName.ToLower())
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
            log4net.ILog log = log4net.LogManager.GetLogger(LogRepository.Name, loggerName);
            //adds the logger as a seperate hierchy, not dependant on any other logger
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;

            //Applies the logger threshold level
            l.Level = l.Hierarchy.LevelMap[levelName];

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

        private static void EnableFilter(string loggerName, string stringToMatch)
        {

            log4net.ILog log = log4net.LogManager.GetLogger(LogRepository.Name, loggerName);
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;
        }

        /// <summary>
        /// To find an appender
        /// </summary>
        /// <param name="appenderName">name of the appender to find</param>
        /// <returns> if not found null will be returned</returns>
        public static log4net.Appender.IAppender FindAppender(string appenderName)
        {
            foreach (log4net.Appender.IAppender appender in log4net.LogManager.GetRepository(LogRepository.Name).GetAppenders())
            {
                if (appender.Name == appenderName)
                {
                    return appender;
                }
            }
            return null;
        }

        ~Log4net()
        {
            IEnumerator cacheNameEnum = LoggingInformation.cacheLogger.GetEnumerator();

            while (cacheNameEnum.MoveNext())
            {
                Close((string)cacheNameEnum.Current);
            }


            cacheNameEnum = LoggingInformation.staticCacheLogger.GetEnumerator();

            while (cacheNameEnum.MoveNext())
            {
                Close((string)cacheNameEnum.Current);
            }

        }

    }
}
