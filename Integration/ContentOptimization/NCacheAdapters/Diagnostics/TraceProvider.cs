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
using System.Collections.Generic;
using System.Text;
using Alachisoft.ContentOptimization.Diagnostics.Logging;
using Alachisoft.NCache.ContentOptimization.Configurations;
using System.IO;
using Alachisoft.NCache.ContentOptimization.Util;
using System.Diagnostics;

namespace Alachisoft.NCache.ContentOptimization.Diagnostics
{
    class FileBasedTraceProvider : ITraceProvider,IDisposable
    {
        private TextWriter writer;
        private object _sync_mutex = new object();
        static FileBasedTraceProvider s_trace;

        static FileBasedTraceProvider()
        {
            s_trace = new FileBasedTraceProvider();
            if (ConfigurationProvider.Settings.EnableTrace)
            {
                string filename = "contentoptimization." + Process.GetCurrentProcess().Id;
                s_trace.Initialize(filename, "ContentOptimizationLogs");
            }
        }

        public static FileBasedTraceProvider Current { get { return s_trace; }  }
        #region ITraceProvider Members

        public void WriteTrace(TraceSeverity level, string categoryName, string message)
        {
            if (ConfigurationProvider.Settings.EnableTrace)
            {
                WriteLogEntry(GetServerityCompatibleString(level), message);
            }
        }

        #endregion

        public void WriteTrace(TraceSeverity level,string message)
        {
            if (ConfigurationProvider.Settings.EnableTrace)
            {
                WriteLogEntry(GetServerityCompatibleString(level), message);
            }
        }

        private string GetServerityCompatibleString(TraceSeverity serverity)
        {
            string servertiyString = "";

            switch (serverity)
            {
                case TraceSeverity.InformationEvent:
                case TraceSeverity.Medium:
                case TraceSeverity.Monitorable:
                case TraceSeverity.Verbose:
                    servertiyString = "information";
                    break;

                case TraceSeverity.WarningEvent:
                    servertiyString = "warning";
                    break;

                case TraceSeverity.CriticalEvent:
                    servertiyString = "critical";
                    break;
                    
                case TraceSeverity.Exception:
                case TraceSeverity.Unexpected:
                    servertiyString = "error";
                    break;

                default:
                    servertiyString = "unspecified";
                    break;
            }
            return servertiyString;
        }

        /// <summary>
        /// True if writer is not instantiated, false otherwise
        /// </summary>
        public bool IsWriterNull
        {
            get
            {
                if (writer == null) return true;
                else return false;
            }
        }

        /// <summary>
        /// Creates logs in installation folder
        /// </summary>
        /// <param name="fileName">name of file</param>
        /// <param name="directory">directory in which the logs are to be made</param>
        public void Initialize(string fileName, string directory)
        {
            Initialize(fileName, null, directory);
        }

        /// <summary>
        /// Creates logs in provided folder
        /// </summary>
        /// <param name="fileName">name of file</param>
        /// <param name="filepath">path where logs are to be created</param>
        /// <param name="directory">directory in which the logs are to be made</param>
        public void Initialize(string fileName, string filepath, string directory)
        {
            lock (_sync_mutex)
            {
                string filename = fileName + "." +
                    Environment.MachineName.ToLower() + "." +
                    DateTime.Now.ToString("dd-MM-yy HH-mm-ss") + @".logs.txt";

                if (filepath == null || filepath == string.Empty)
                {
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
                }

                try
                {
                    filepath = Path.Combine(filepath, directory);
                    if (!Directory.Exists(filepath)) Directory.CreateDirectory(filepath);

                    filepath = Path.Combine(filepath, filename);

                    writer = TextWriter.Synchronized(new StreamWriter(filepath, false));
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Write to log file
        /// </summary>
        /// <param name="module">module</param>
        /// <param name="logText">text</param>
        public void WriteLogEntry(string severity,  string logText)
        {
            if (writer != null)
            {
                int space2 = 40;
                string line = null;
                severity = "[" + severity + "]";
                line = System.DateTime.Now.ToString("dd-MM-yy HH:mm:ss:ffff") + ":  " + severity.PadRight(space2, ' ')+ logText;
                lock (_sync_mutex)
                {
                    writer.WriteLine(line);
                    writer.Flush();
                }
            }
        }

        /// <summary>
        /// Close writer
        /// </summary>
        public void Close()
        {
            lock (_sync_mutex)
            {
                if (writer != null)
                {
                    lock (writer)
                    {
                        writer.Close();
                        writer = null;
                    }
                }
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion
    }
}
