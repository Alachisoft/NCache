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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Configuration;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Integrations.EntityFramework.Config;


namespace Alachisoft.NCache.Integrations.EntityFramework.Util
{
    internal sealed class Logger : IDisposable
    {
        /// <summary>
        /// Specifies what messages to output
        /// </summary>
        public enum LoggingLevel
        {
            /// <summary>
            /// Output no error or debug messages
            /// </summary>
            Off = 0,
            /// <summary>
            /// Output error messages only
            /// </summary>
            Error,
            /// <summary>
            /// Output error, and detailed debug messages
            /// </summary>
            Debug
        }

        private object _sync_point = new object();
        private StreamWriter _writer = null;

        /// <summary>
        /// Static constructor
        /// </summary>
        static Logger()
        {
            Instance = new Logger();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private Logger()
        {
            this.Level = Application.Instance.LoggingLevel;
        }
        
        /// <summary>
        /// Create a log file
        /// </summary>
        private void CreateLogFile()
        {
            if (this._writer != null)
            {
                return;
            }

            ///If error logging is turned off.
            if (this.Level == LoggingLevel.Off)
            {
                return;
            }

            string fileName = "\\";

            ///If install dir is specified, we will save the logs in 
            ///ncache\logs-files\efcachinglogs folder, otherwise it will be saved
            ///where application is running
            if (!AppUtil.LogDir.IsNullOrEmpty())
            {
                fileName = Path.Combine(AppUtil.LogDir,
                    "log-files\\EfCachingLogs\\");

                if (!Directory.Exists(fileName))
                {
                    Directory.CreateDirectory(fileName);
                }
            }

            string applicationId = string.Empty;
            if (!Application.Instance.ApplicationId.IsNullOrEmpty())
            {
                applicationId = Application.Instance.ApplicationId + ".";
            }

            fileName = Path.Combine(fileName,
                string.Format("{0}{1}.{2}.{3}.log.txt", applicationId, Environment.MachineName.ToLower(), Process.GetCurrentProcess().Id.ToString(), DateTime.Now.ToString("dd-MM-yy HH-mm-ss")));

            try
            {
                this._writer = new StreamWriter(fileName, false);
                this._writer.AutoFlush = true;
            }
            catch (Exception exc)
            {
                try
                {
                    EventLog.WriteEntry("EFCachingProvider", "An error occurred while initializing logger.\r\n" + exc.ToString());
                }
                ///If an exception occurs then we have no other way but to inform user.
                ///We will throw the orignal exception that occured when creating logger
                catch (System.ComponentModel.Win32Exception) { throw exc; }
                catch (Exception) { throw exc; }
            }
        }

        /// <summary>
        /// Initialize logging
        /// </summary>
        /// <remarks>Logger is initialized only once.
        /// No action will be taken if logger has already been initialized</remarks>
        public static void Initialize()
        {
            Instance.CreateLogFile();
        }

        /// <summary>
        /// Current instance of Logger
        /// </summary>
        public static Logger Instance { get; private set; }

        /// <summary>
        /// Get tracing level
        /// </summary>
        public LoggingLevel Level { get; private set; }

        /// <summary>
        /// Write debug information to trace file
        /// </summary>
        /// <param name="message">Message to write</param>
        public void TraceDebug(string message)
        {
            if (this._writer == null || this.Level < LoggingLevel.Debug)
            {
                return;
            }

            this.WriteMessage(LoggingLevel.Debug, 2, message);
        }

        /// <summary>
        /// Write error message to trace file
        /// </summary>
        /// <param name="message">Message to write</param>
        public void TraceError(string message)
        {
            if (this._writer == null || this.Level < LoggingLevel.Error)
            {
               return;
            }

            this.WriteMessage(LoggingLevel.Error, 2, message);
        }

        /// <summary>
        /// Write trace in log file
        /// </summary>
        /// <param name="level">Trace leve</param>
        /// <param name="skipFrame">Number of frames up the stack to skip</param>
        /// <param name="message">Message to print</param>
        private void WriteMessage(LoggingLevel level, int skipFrame, string message)
        {
            try
            {
                lock (this._sync_point)
                {
                    if (this._writer != null)
                    {
                        StackFrame frame = new StackFrame(skipFrame, true);
                        MethodBase method = frame.GetMethod();

                        this._writer.WriteLine(DateTime.Now.ToString() +
                            ":  " +
                            string.Format("[{0}]\t", level.ToString()) +
                            string.Format("{0}.{1}\t", method.DeclaringType.Name, method.Name) +
                            message);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this._writer != null)
            {
                lock (this._sync_point)
                {
                    if (this._writer != null)
                    {
                        try
                        {
                            this._writer.Close();
                        }
                        catch (ObjectDisposedException) { }

                        this._writer.Dispose();
                        this._writer = null;

                        GC.SuppressFinalize(this);
                    }

                    this.Level = LoggingLevel.Off;
                }
            }
        }

        #endregion
    }
}
