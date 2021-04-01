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
using System.Text;
using System.IO;
using System.Reflection;
using System.Net.Sockets;

using log4net.Appender;
using System.Net;
using Alachisoft.NCache.Common.Util;
using log4net;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Common.Logger
{
    public class NCacheLogger : ILogger
    {
        private readonly byte[] log4netXML = Encoding.ASCII.GetBytes("<?xml version=\"1.0\"?> <configuration>  <configSections>   <section name=\"log4net\" type=\"log4net.Config.Log4NetConfigurationSectionHandler, log4net, Version=1.2.10.0, Culture = neutral, PublicKeyToken=1b44e1d426115821 \"/> </configSections>   <log4net></log4net> </configuration>");
        string _cacheserver="NCache";

        public log4net.Core.Level criticalInfo = new log4net.Core.Level(5000000, "CRIT", "INFO");
        public log4net.Core.Level devInfo = new log4net.Core.Level(5000001, "DEV", "DEV");

        private log4net.ILog log;

        private const string DIRNAME = @"log-files";

        /// <summary>Path of the configuration folder.</summary>
        private static string s_configDir = "";
        private string _path = "";
        private static int bufferDefaultSize = 1;
        private string _loggerName = null;

        private readonly string NEW_LINE = "\r\n";
        private static string _nodeIP = ""; //Ehsan: To add nodeIP in the logs files name
        /// <summary>
        /// Scans the registry and locates the configuration file.
        /// </summary>
        static NCacheLogger()
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

            bufferDefaultSize = ServiceConfiguration.BufferSize;
                
          
            if (ServiceConfiguration.BindToIP != null)
            {
                _nodeIP = ServiceConfiguration.BindToIP.ToString();
            }
            try
            {
                if (string.IsNullOrEmpty(_nodeIP))
                {
                    string strHostName = System.Net.Dns.GetHostName();
                    IPAddress[] addressList = System.Net.Dns.GetHostEntry(strHostName).AddressList;

                    foreach (IPAddress ip in addressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            _nodeIP = ip.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception) { _nodeIP = System.Environment.MachineName; }
        }

        #region ILogger Members

        public string Initialize(System.Collections.IDictionary properties, string partitionID, string cacheName)
        {
            return Initialize(properties, partitionID, cacheName, false, false);
        }

        public string Initialize(System.Collections.IDictionary properties, string partitionID, string cacheName, bool isStartedAsMirror, bool inproc)
        {
            if (log != null)
                throw new Exception("Multiple Initialize calls for same logger");

            MemoryStream logStream = new MemoryStream(log4netXML);
            log4net.Config.XmlConfigurator.Configure(Log4net.LogRepository, logStream);

            if (_loggerName != null)
                return _loggerName;
            try
            {
                _loggerName = cacheName;
                if (partitionID != null && partitionID.Length > 0)
                    _loggerName += "-" + partitionID;

                if (isStartedAsMirror)
                    _loggerName += "-" + "replica";

                if (inproc && !isStartedAsMirror) _loggerName += "." + System.Diagnostics.Process.GetCurrentProcess().Id;

                string LogExceptions = "";

                if (_loggerName == "LogExceptions")
                    LogExceptions = "\\LogExceptions";
                bool defaultPath = true;
                string initialPath = "";

                if (properties.Contains("log-path"))
                {
                    initialPath = Convert.ToString(properties["log-path"]);
                    if(!String.IsNullOrEmpty(initialPath))
                        defaultPath = !IsValidLocation(initialPath, cacheName);
                }


                if (defaultPath)
                    initialPath = GetLogPath();

                string fileName = initialPath + LogExceptions + Path.DirectorySeparatorChar + _loggerName + "_" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second +"_" +_nodeIP + ".txt"; ;

                AddAppender(CreateBufferAppender(fileName, false, false));


                if (properties != null)
                {
                    if (properties.Contains("trace-errors"))
                    {
                        if (Convert.ToBoolean(properties["trace-errors"]))
                        {
                            SetLevel(LoggerLevel.Error);
                        }
                    }

                    if (properties.Contains("trace-notices"))
                    {
                        if (Convert.ToBoolean(properties["trace-notices"]))
                        {
                            SetLevel(LoggerLevel.Info);
                        }
                    }

                    if (properties.Contains("trace-warnings"))
                        if (Convert.ToBoolean(properties["trace-warnings"]))
                        {
                            SetLevel(LoggerLevel.Warn);
                        }

                    if (properties.Contains("trace-debug"))
                        if (Convert.ToBoolean(properties["trace-debug"]))
                        {
                            SetLevel(LoggerLevel.All);
                        }

                    if (properties.Contains("enabled"))
                    {
                        if (!Convert.ToBoolean(properties["trace-errors"]))
                        {
                            SetLevel(LoggerLevel.Off);
                        }
                    }
                }
                else
                {
                    SetLevel(LoggerLevel.Warn);
                }

            }
            catch (Exception e)
            {
                AppUtil.LogEvent(_cacheserver, "Failed to open log. " + e, System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
            }

            return _loggerName;

        }

        public void Initialize(LoggerNames loggerName)
        {

            if (loggerName != LoggerNames.Licence)
            {
                Initialize(loggerName, null);
            }

        }

        public void Initialize(LoggerNames loggerNameEnum, string cacheName)
        {

            if (log != null)
                throw new Exception("Multiple Initialize calls for same logger");

            MemoryStream logStream = new MemoryStream(log4netXML);
            log4net.Config.XmlConfigurator.Configure(Log4net.LogRepository, logStream);

            _loggerName = loggerNameEnum.ToString();

            string filename = _loggerName;
            if (loggerNameEnum == LoggerNames.ClientLogs)
            {
                filename = filename + "." + cacheName + "." + System.Diagnostics.Process.GetCurrentProcess().Id;

                // changing the name here will invalidate static log checks automatically since LoggerName == ClientLogs
                _loggerName = cacheName + System.Diagnostics.Process.GetCurrentProcess().Id;
            }
            else if (loggerNameEnum == LoggerNames.CacheHostLogs)
            {
                filename = cacheName + "." + loggerNameEnum.ToString();
            }
            else if (loggerNameEnum == LoggerNames.ServiceLogs)
            {
                filename = filename + "." + +System.Diagnostics.Process.GetCurrentProcess().Id;
            }
            else if (loggerNameEnum == LoggerNames.LicensingLog)
            {
                filename = "NCService" + "." + loggerNameEnum.ToString();
            }
            else if (loggerNameEnum == LoggerNames.UsageInfoLog)
            {
                filename = loggerNameEnum.ToString();
            }
            else
            {
                filename = cacheName;
            }

            //if (loggerNameEnum != LoggerNames.ClientLogs)
            //{
            //    _loggerName = _loggerName + DateTime.Now;
            //}


            filename = string.IsNullOrEmpty(filename) ? string.Empty : filename +  ".";
            if (loggerNameEnum == LoggerNames.LicensingLog)
            {
                filename += DateTime.Now.ToString("dd-MM-yy_HH-mm-ss") + "_" + _nodeIP + @".bin";
            }
            else
            {
                filename += DateTime.Now.ToString("dd-MM-yy_HH-mm-ss") + "_" + _nodeIP + @".txt";
            }

            string filepath = "";

            //if (defaultPath)
            //{

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
            //}
            try
            {

                filepath = Path.Combine(filepath, loggerNameEnum.ToString());

                if (!Directory.Exists(filepath)) Directory.CreateDirectory(filepath);

                filepath = Path.Combine(filepath, filename);
                AddAppender(CreateBufferAppender(filepath, false, (loggerNameEnum == LoggerNames.eventlogs)));

            }
            catch (Exception)
            {
                throw;
            }

        }
        public void InitializeAPILogging(LoggerNames loggerNameEnum, string cacheName, string filePath)
        {

            if (log != null)
                throw new Exception("Multiple Initialize calls for same logger");

            MemoryStream logStream = new MemoryStream(log4netXML);
            log4net.Config.XmlConfigurator.Configure(Log4net.LogRepository, logStream);

            _loggerName = loggerNameEnum.ToString();

            string filename = _loggerName;
            if (loggerNameEnum == LoggerNames.APILogs)
            {
                filename = cacheName;
            }
            filename = string.IsNullOrEmpty(filename) ? string.Empty : filename + ".";
            filename += DateTime.Now.ToString("dd-MM-yy HH-mm-ss") + "_" + _nodeIP + @".txt";
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            try
            {

                filePath = Path.Combine(filePath, filename);
                AddAppender(CreateBufferAppender(filePath, true, false));
            }
            catch (Exception)
            {
                throw;
            }

        }

        public void InitializeEventsLogging()
        {
            string fileName;
            string filePath;
            _loggerName = LoggerNames.eventlogs.ToString();

            if (log != null)
                throw new Exception("Multiple Initialize calls for same logger");

            MemoryStream logStream = new MemoryStream(log4netXML);
            log4net.Config.XmlConfigurator.Configure(Log4net.LogRepository, logStream);

            filePath = "";
            fileName = LoggerNames.eventlogs.ToString() + ".txt";
            
            if (!DirectoryUtil.SearchGlobalDirectory("log-files", false, out filePath))
            {
                try
                {
                    DirectoryUtil.SearchLocalDirectory("log-files", true, out filePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to initialize the log file", ex);
                }
            }

            filePath = Path.Combine(filePath, LoggerNames.eventlogs.ToString());

            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            try
            {
                filePath = Path.Combine(filePath, fileName);
                AddAppender(CreateBufferAppender(filePath, false, true));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SetLevel(string levelName)
        {
            //adds the logger as a seperate hierchy, not dependant on any other logger
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;

            //Applies the logger threshold level
            l.Level = l.Hierarchy.LevelMap[levelName.ToLower()];
        }

        public void Error(string message)
        {

            log.Error(message);

        }

        public void Fatal(string message)
        {
            log.Fatal(message);
        }

        public void CriticalInfo(string message)
        {
            log.Logger.Log(null, criticalInfo, message, null);
        }

        public void Info(string message)
        {
            log.Info(message);
        }

        public void Debug(string message)
        {
            log.Debug(message);
        }

        public void Warn(string message)
        {
            log.Warn(message);
        }

        public bool IsInfoEnabled
        {
            get
            {
                return log.IsDebugEnabled;
            }
        }

        public bool IsErrorEnabled
        {
            get
            {
                return log.IsErrorEnabled;
            }
        }

        public bool IsWarnEnabled
        {
            get
            {

                return log.IsWarnEnabled;

            }

        }

        public bool IsDebugEnabled
        {
            get
            {

                return log.IsDebugEnabled;

            }

        }

        public bool IsFatalEnabled
        {
            get
            {

                return log.IsFatalEnabled;

            }

        }

        #endregion

        internal bool IsValidLocation(string location, string cache)
        {
            try
            {
                Exception ex = new Exception("Invalid Directory specified.");
                if (string.IsNullOrEmpty(location))
                {
                    throw ex;
                }
                log4net.LogManager.GetRepository(Log4net.LogRepository.Name).LevelMap.Add(criticalInfo);

                if (System.IO.Directory.Exists(location))
                    return true;
                else
                {
                    throw ex;
                }
            }
            catch (Exception)
            {
                AppUtil.LogEvent(_cacheserver, "Custom log path specified for cache :\"" + cache + "\"does not exist", System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                
            }
            return false;
        }

        internal string GetLogPath()
        {
            if (_path.Length < 1)
            {
                _path = s_configDir;
            }
            try
            {
                log4net.LogManager.GetRepository(Log4net.LogRepository.Name).LevelMap.Add(criticalInfo);

                if (!System.IO.Directory.Exists(_path))
                    System.IO.Directory.CreateDirectory(_path);
            }
            catch (Exception e)
            {
                AppUtil.LogEvent(_cacheserver, e.ToString(), System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
            }
            return _path;
        }

        /// <summary>
        /// Add a desired appender to the logger
        /// </summary>
        /// <param name="loggerName">Name of the logger to which the appender is to be added</param>
        /// <param name="appender">Appender to add to the logger</param>
        private void AddAppender(log4net.Appender.IAppender appender)
        {
            log = log4net.LogManager.GetLogger(Log4net.LogRepository.Name, _loggerName);
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;
            l.AddAppender(appender);
        }

        private void RemoveAllAppender(log4net.ILog log)
        {
            log4net.Repository.Hierarchy.Logger l = (log4net.Repository.Hierarchy.Logger)log.Logger;

            l.RemoveAllAppenders();
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
        private log4net.Appender.IAppender CreateBufferAppender(string fileName, bool apiLogs, bool eventLogs)
        {
            log4net.Appender.BufferingForwardingAppender appender = new BufferingForwardingAppender();
            appender.Name = "BufferingForwardingAppender" + _loggerName;
            //Pick from config
            int bufferSize = bufferDefaultSize;
            ReadConfig(out bufferSize);
            if (bufferSize == bufferDefaultSize)
                ReadClientConfig(out bufferSize);

            if (bufferSize < 1)
                bufferSize = bufferDefaultSize;

            appender.BufferSize = bufferSize;

            //Threshold is maintained by the logger rather than the appenders
            appender.Threshold = log4net.Core.Level.All;

            //Adds the appender to which it will pass on all the logging levels upon filling up the buffer

            appender.AddAppender(CreateRollingFileAppender(fileName, apiLogs, eventLogs));


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
        private log4net.Appender.IAppender CreateRollingFileAppender(string fileName, bool apiLogs, bool eventLogs)
        {
            log4net.Appender.RollingFileAppender appender = new log4net.Appender.RollingFileAppender();
            appender.Name = "RollingFileAppender" + _loggerName;
            appender.File = fileName;
            //doesnt matter since all files are created with a new name
            appender.AppendToFile = true;

            appender.RollingStyle = RollingFileAppender.RollingMode.Size;
            appender.MaximumFileSize = "5MB";
            appender.MaxSizeRollBackups = -1;
            
            appender.Threshold = log4net.Core.Level.All;

            log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();

            if (!apiLogs && !eventLogs)
            {
                layout.ConversionPattern = "%-27date{ISO8601}" + "\t%-45.42appdomain" + "\t%-43logger" + "\t%-42thread" + "\t%-9level" + "\t%message" + "%newline";

                layout.Header = "TIMESTAMP                  \tAPPDOMAIN                                    \tLOGGERNAME                                 \tTHREADNAME                                \tLEVEL    \tMESSAGE\r\n";
            }
            else if (apiLogs)
            {
             
                appender.Name = LoggerNames.APILogs.ToString().ToLower()+"_" + appender.Name;
                layout.ConversionPattern = "%message" + "%newline";
                layout.Header = "TIMESTAMP                     \t\t    SERVER              CLIENTIP            ProcessID           				\tExecutionTime   \tMethod                                          														 											             \t\t\tParameters                                                                                                                                                                                                                                 Exception\r\n";
               
            }


            else if (eventLogs)
            {
                layout.ConversionPattern = "%message" + "%newline";
                appender.LockingModel = new log4net.Appender.FileAppender.MinimalLock();
                if (!File.Exists(fileName))
                    layout.Header = "TIMESTAMP                                   SOURCE                  EVENTID                 LEVEL                   MESSAGE\r\n";
            }

            layout.Footer = "END \n";
            layout.ActivateOptions();
            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }

        public bool[] ReadConfig(out int bufferAppender)
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
            }

        }

        public bool[] ReadClientConfig(out int bufferAppender)
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
            }

        }

        #region ILogger Members


        public void Close()
        {

            SetLevel("OFF");

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
            this.RemoveAllAppender(log);

        }

        #endregion

        public void FlushAPILogs()

        {
            IAppender[] appenders = log.Logger.Repository.GetAppenders();
             for (int i=0; i<appenders.Length; i++)
            {
                RollingFileAppender rollingFileAppender = appenders[i] as RollingFileAppender;
                if (rollingFileAppender != null)
                {
                    string appenderName = rollingFileAppender.Name;
                    string[] explodeString = appenderName.Split('_');
                    if (explodeString.Length>0)
                    {
                        if (explodeString[0].Equals(LoggerNames.APILogs.ToString().ToLower(),StringComparison.InvariantCultureIgnoreCase))
                        {
                            rollingFileAppender.Close();
                            try
                            {
                                var root = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(Log4net.LogRepository.Name)).Root;
                                root.RemoveAppender(rollingFileAppender);
                            }
                            catch (Exception ex)
                            {

                            }

                        }
                    }
                }
            }
        }

        public void Flush()
        {
            IAppender[] logAppenders = log.Logger.Repository.GetAppenders();

            for (int i = 0; i < logAppenders.Length; i++)
            {
                if (logAppenders[i] != null)
                {
                    BufferingAppenderSkeleton buffered = logAppenders[i] as BufferingAppenderSkeleton;
                    if (buffered is BufferingForwardingAppender)
                    {
                        ((BufferingForwardingAppender)buffered).Flush();
                    }
                }
            }

        }
     
        #region ILogger Members
        public void Error(string module, string message)
        {

            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Error(line);

        }

        public void Fatal(string module, string message)
        {

            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Fatal(line);

        }

        public void CriticalInfo(string module, string message)
        {

            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            CriticalInfo(line);

        }

        public void APIlInfo(string time, string server, string clientIp, string processID, string executionTime, string Method, string Parameters, string Exception)
        {
            int space2 = 20;
            StringBuilder buffer = new StringBuilder();
            buffer.Append( time.PadRight(space2 +space2) + server.PadRight(space2) + clientIp.PadRight(space2) + processID.PadRight(space2 + space2) + executionTime.PadRight(space2) + Method.PadRight(150+space2));
            int line = buffer.ToString().Length;
            string[] logParams = Parameters.Split(',');
            for (int i = 0; i < logParams.Length; i++ )
            {
                if (i == 0)
                {
                    buffer.Append(logParams[i].PadRight(400) + Exception + "\n");
                }
                else
                    buffer.Append(logParams[i].PadRight(space2).PadLeft(line+space2) + "\n");
            }
            Info(buffer.ToString());

        }

        public void EventLog(string time, string source, string eventid, string level, string message)
        {
            int space2 = 10;
            StringBuilder buffer = new StringBuilder();
            buffer.Append(time.PadRight(4 * space2) + "\t" + source.PadRight(2 * space2) + "\t" + eventid.PadRight(2 * space2) + "\t" + level.PadRight(2 * space2) + "\t" + message.PadRight(2 * space2));
            Info(buffer.ToString());
        }


        public void Info(string module, string message)
        {

            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Info(line);

        }

        public void Debug(string module, string message)
        {

            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Debug(line);

        }

        public void Warn(string module, string message)
        {

            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            Warn(line);

        }

        public void DevTrace(string message)
        {

            log.Logger.Log(null, devInfo, message, null);

        }

        public void DevTrace(string module, string message)
        {

            int space2 = 40;

            if (module.Length == 0)
                space2 = 4;

            string line = null;

            line = module.PadRight(space2, ' ') + message;

            DevTrace(line);

        }

        #endregion



    }
}
